using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The post (سمت) of a user: optional, one per user, independent of the role. The two system posts (Line, Staff)
    /// are guaranteed to exist; the rest are plain labels. No business rule hangs on a post yet.
    /// </summary>
    public class UserPostTests : ApiTestBase
    {
        public UserPostTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..18];
        private static long Line => PostCatalog.IdOf(PostCode.Line);
        private static long Staff => PostCatalog.IdOf(PostCode.Staff);

        private async Task<HttpResponseMessage> CreateUserWithPostAsync(string userName, long? postId, string password = "Passw0rd!x") =>
            await SendAsync(HttpMethod.Post, "/api/Users", new { userName, password, firstName = "A", lastName = "B", personnelCode = "PC-" + userName, postId }, bearer: await AdminAsync());

        private async Task<JsonElement> UserAsync(long id) =>
            await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Users/{id}", bearer: await AdminAsync()));

        private static object UpdateBody(string userName, long? postId, bool isActive = true) =>
            new { userName, firstName = "A", lastName = "B", personnelCode = "PC-" + userName, isActive, postId };

        // ------------------------------------------------------------------------------- the two system posts

        [Fact]
        public async Task The_two_system_posts_exist_with_their_persian_titles()
        {
            var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/Post?pageSize=100", bearer: await AdminAsync()));
            var posts = DataOf(list).EnumerateArray().ToDictionary(p => p.GetProperty("id").GetInt64());
            Assert.Equal("Line", posts[Line].GetProperty("code").GetString());
            Assert.Equal("Staff", posts[Staff].GetProperty("code").GetString());
            Assert.Equal("صف", posts[Line].GetProperty("title").GetString());
            Assert.Equal("ستاد", posts[Staff].GetProperty("title").GetString());
        }

        [Fact]
        public async Task Any_signed_in_user_can_read_the_posts_but_only_a_permitted_one_can_change_them()
        {
            var admin = await AdminAsync();
            var userName = "pplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/Post", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, "/api/Post", new { title = Uniq("p") }, bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/Post")).StatusCode);
        }

        // ------------------------------------------------------------------------------- a user's post

        [Fact]
        public async Task A_user_can_be_created_with_a_post_or_without_one()
        {
            var with = await CreateUserWithPostAsync(Uniq("pu"), Line);
            Assert.Equal(HttpStatusCode.Created, with.StatusCode);
            var json = await ReadDataAsync(with);
            Assert.Equal(Line, json.GetProperty("postId").GetInt64());
            Assert.Equal("Line", json.GetProperty("postCode").GetString());
            Assert.Equal("صف", json.GetProperty("postTitle").GetString());

            var without = await CreateUserWithPostAsync(Uniq("pn"), null);
            Assert.Equal(HttpStatusCode.Created, without.StatusCode);
            var none = await ReadDataAsync(without);
            Assert.Equal(JsonValueKind.Null, none.GetProperty("postId").ValueKind);
            Assert.Equal(JsonValueKind.Null, none.GetProperty("postTitle").ValueKind);
        }

        [Fact]
        public async Task Update_changes_and_clears_the_post_and_the_user_list_and_detail_show_it()
        {
            var userName = Uniq("pup");
            var id = (await ReadDataAsync(await CreateUserWithPostAsync(userName, Line))).GetProperty("id").GetInt64();

            var changed = await SendAsync(HttpMethod.Put, $"/api/Users/{id}", UpdateBody(userName, Staff), bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            Assert.Equal("Staff", (await ReadDataAsync(changed)).GetProperty("postCode").GetString());
            Assert.Equal(Staff, (await UserAsync(id)).GetProperty("postId").GetInt64());

            var item = DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Users?userName={userName}", bearer: await AdminAsync())))[0];
            Assert.Equal(Staff, item.GetProperty("postId").GetInt64());
            Assert.Equal("ستاد", item.GetProperty("postTitle").GetString());

            // an update replaces the field: sending no post clears it
            var cleared = await SendAsync(HttpMethod.Put, $"/api/Users/{id}", UpdateBody(userName, null), bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, cleared.StatusCode);
            Assert.Equal(JsonValueKind.Null, (await UserAsync(id)).GetProperty("postId").ValueKind);
        }

        [Fact]
        public async Task An_unknown_or_inactive_post_is_refused_with_the_catalog_codes()
        {
            var admin = await AdminAsync();
            var unknown = await CreateUserWithPostAsync(Uniq("px"), 99999999);
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));

            var zero = await CreateUserWithPostAsync(Uniq("pz"), 0);
            Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
            Assert.Contains("postId", ErrorFieldsOf(await ReadJsonAsync(zero)));

            var custom = (await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Post", new { title = Uniq("inactive") }, bearer: admin))).GetProperty("id").GetInt64();
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Post/{custom}", bearer: admin)).StatusCode);
            var inactive = await CreateUserWithPostAsync(Uniq("pi"), custom);
            Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(inactive)));
        }

        [Fact]
        public async Task A_post_with_active_users_cannot_be_deactivated_and_a_user_keeps_a_post_that_was_deactivated_later()
        {
            var admin = await AdminAsync();
            var custom = (await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Post", new { title = Uniq("held"), code = "HELD" }, bearer: admin))).GetProperty("id").GetInt64();
            var userName = Uniq("ph");
            var userId = (await ReadDataAsync(await CreateUserWithPostAsync(userName, custom))).GetProperty("id").GetInt64();

            var blocked = await SendAsync(HttpMethod.Delete, $"/api/Post/{custom}", bearer: admin);
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Equal("has_active_dependents", ErrorCodeOf(await ReadJsonAsync(blocked)));

            // deactivate the user, then the post can go; the (inactive) user still shows the post it had
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Users/{userId}", bearer: admin)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Post/{custom}", bearer: admin)).StatusCode);
            var user = await UserAsync(userId);
            Assert.Equal(custom, user.GetProperty("postId").GetInt64());
            Assert.Equal("HELD", user.GetProperty("postCode").GetString());

            // an update that leaves the same inactive post in place is fine
            var keep = await SendAsync(HttpMethod.Put, $"/api/Users/{userId}", UpdateBody(userName, custom, isActive: false), bearer: admin);
            Assert.Equal(HttpStatusCode.OK, keep.StatusCode);
        }

        [Fact]
        public async Task The_post_is_independent_of_the_role()
        {
            var admin = await AdminAsync();
            var role = await CreateRoleAsync(admin, "PostRole" + Guid.NewGuid().ToString("N")[..4], "Brands.View");
            var userName = Uniq("pr");
            var userId = (await ReadDataAsync(await CreateUserWithPostAsync(userName, Staff))).GetProperty("id").GetInt64();
            await AssignRoleAsync(admin, userId, role);

            var user = await UserAsync(userId);
            Assert.Equal("Staff", user.GetProperty("postCode").GetString());
            Assert.Equal(1, user.GetProperty("roles").GetArrayLength());

            // changing the role does not touch the post, changing the post does not touch the role
            await RemoveRoleAsync(admin, userId, role);
            Assert.Equal("Staff", (await UserAsync(userId)).GetProperty("postCode").GetString());
            await SendAsync(HttpMethod.Put, $"/api/Users/{userId}", UpdateBody(userName, Line), bearer: admin);
            Assert.Equal(0, (await UserAsync(userId)).GetProperty("roles").GetArrayLength());
        }

        // ------------------------------------------------------------------------------- filter, login

        [Fact]
        public async Task The_user_list_filters_by_post()
        {
            var admin = await AdminAsync();
            var tag = Guid.NewGuid().ToString("N")[..6];
            var t1 = (await ReadDataAsync(await CreateUserWithPostAsync($"pf_t1_{tag}", Line))).GetProperty("id").GetInt64();
            var t2 = (await ReadDataAsync(await CreateUserWithPostAsync($"pf_t2_{tag}", Line))).GetProperty("id").GetInt64();
            var m = (await ReadDataAsync(await CreateUserWithPostAsync($"pf_m_{tag}", Staff))).GetProperty("id").GetInt64();
            var none = (await ReadDataAsync(await CreateUserWithPostAsync($"pf_n_{tag}", null))).GetProperty("id").GetInt64();

            var technicians = await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Users?postId={Line}&search=pf_&pageSize=100", bearer: admin));
            var ids = DataOf(technicians).EnumerateArray().Select(u => u.GetProperty("id").GetInt64()).ToHashSet();
            Assert.Contains(t1, ids); Assert.Contains(t2, ids);
            Assert.DoesNotContain(m, ids); Assert.DoesNotContain(none, ids);
            Assert.All(DataOf(technicians).EnumerateArray(), u => Assert.Equal(Line, u.GetProperty("postId").GetInt64()));
        }

        [Fact]
        public async Task The_users_of_a_role_show_their_posts_and_login_returns_the_post()
        {
            var admin = await AdminAsync();
            var role = await CreateRoleAsync(admin, "PostLogin" + Guid.NewGuid().ToString("N")[..4]);
            var userName = Uniq("pl");
            var userId = (await ReadDataAsync(await CreateUserWithPostAsync(userName, Staff))).GetProperty("id").GetInt64();
            await AssignRoleAsync(admin, userId, role);

            var members = DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Role/{role}/users", bearer: admin)));
            Assert.Equal("ستاد", members[0].GetProperty("postTitle").GetString());

            var login = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName, password = "Passw0rd!x" }));
            Assert.Equal(Staff, login.GetProperty("postId").GetInt64());
            Assert.Equal("Staff", login.GetProperty("postCode").GetString());
            Assert.Equal("ستاد", login.GetProperty("postTitle").GetString());

            // the administrator has no post: the fields are null, not missing
            var adminLogin = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = AdminPassword }));
            Assert.Equal(JsonValueKind.Null, adminLogin.GetProperty("postId").ValueKind);
        }

        // ------------------------------------------------------------------------------- the guarantee

        [Fact]
        public async Task The_guard_puts_back_a_system_post_that_was_deactivated_recoded_or_removed_and_keeps_edited_titles()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var guard = scope.ServiceProvider.GetRequiredService<ISystemPostsGuard>();
            Assert.Equal((0, 0), await guard.EnsureAsync());   // intact: nothing to do

            var line = await db.Posts.SingleAsync(p => p.PostId == Line);
            var staff = await db.Posts.SingleAsync(p => p.PostId == Staff);
            var editedTitle = Uniq("edited");
            line.IsActive = false; line.Title = editedTitle; line.Code = "Hacked";
            db.Posts.Remove(staff);
            await db.SaveChangesAsync();

            Assert.Equal((1, 1), await guard.EnsureAsync());
            db.ChangeTracker.Clear();

            var rows = await db.Posts.IncludingInactive().Where(p => p.PostId <= Staff).OrderBy(p => p.PostId).ToListAsync();
            Assert.Equal(new[] { "Line", "Staff" }, rows.Select(r => r.Code).ToArray());
            Assert.All(rows, r => Assert.True(r.IsActive));
            Assert.Equal(editedTitle, rows[0].Title);                         // an edited title is never overwritten
            Assert.Equal("ستاد", rows[1].Title);                               // a re-created row gets the initial title
            Assert.Equal((0, 0), await guard.EnsureAsync());                  // and the guard is idempotent

            line = await db.Posts.SingleAsync(p => p.PostId == Line);
            line.Title = "صف"; await db.SaveChangesAsync();                   // leave the shared database as it was found
        }
    }
}
