using EMS.Application.Common.Security;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>What the "role access" screen needs: the catalog with Persian names, a role's permissions by id, the protected administrator role and "me".</summary>
    public class PermissionManagementTests : ApiTestBase
    {
        public PermissionManagementTests(EmsApiFactory factory) : base(factory) { }

        // these tests are about the exact permissions of a role
        protected override bool RolesSeeEveryDepartment => false;

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private async Task<HttpResponseMessage> AsAdminAsync(HttpMethod method, string url, object? body = null) => await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<(long RoleId, string RoleName, long UserId, string UserName)> NewRoleWithUserAsync(params string[] permissions)
        {
            var admin = await AdminAsync();
            var name = "pm_" + Guid.NewGuid().ToString("N")[..10];
            var roleId = await CreateRoleAsync(admin, "role_" + name, permissions);
            var userId = await CreateUserAsync(admin, name, "Passw0rd!x");
            await AssignRoleAsync(admin, userId, roleId);
            return (roleId, "role_" + name, userId, name);
        }

        private static List<JsonElement> PermissionsOf(JsonElement groupsOwner) =>
            groupsOwner.EnumerateArray().SelectMany(g => g.GetProperty("permissions").EnumerateArray()).ToList();

        private async Task<long> AdminRoleIdAsync()
        {
            var roles = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/Role?pageSize=100"));
            return roles.EnumerateArray().First(r => r.GetProperty("name").GetString() == "Admin").GetProperty("id").GetInt64();
        }

        // ------------------------------------------------------------------------------------------ catalog

        [Fact]
        public async Task The_catalog_lists_every_permission_once_with_a_persian_title_for_any_signed_in_user()
        {
            var user = await NewRoleWithUserAsync();     // a role without any permission
            var token = (await LoginAsync(user.UserName, "Passw0rd!x")).AccessToken;

            var response = await SendAsync(HttpMethod.Get, "/api/Permission", bearer: token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var groups = await ReadDataAsync(response);
            var items = PermissionsOf(groups);

            Assert.Equal(Permissions.All.OrderBy(p => p, StringComparer.Ordinal), items.Select(i => i.GetProperty("name").GetString()).OrderBy(p => p, StringComparer.Ordinal));
            Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.GetProperty("title").GetString())));
            Assert.All(groups.EnumerateArray(), g => Assert.False(string.IsNullOrWhiteSpace(g.GetProperty("title").GetString())));

            var equipment = groups.EnumerateArray().First(g => g.GetProperty("key").GetString() == "Equipments");
            Assert.Equal("تجهیزات", equipment.GetProperty("title").GetString());
            var view = equipment.GetProperty("permissions").EnumerateArray().First(p => p.GetProperty("name").GetString() == "Equipments.View");
            Assert.Equal("مشاهده تجهیزات", view.GetProperty("title").GetString());
            Assert.Equal("View", view.GetProperty("action").GetString());
            Assert.False(string.IsNullOrWhiteSpace(view.GetProperty("description").GetString()));

            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/Permission")).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ a role's permissions

        [Fact]
        public async Task A_roles_permissions_are_read_and_replaced_by_id_and_the_change_reaches_its_users_at_once()
        {
            var r = await NewRoleWithUserAsync("Skills.View");
            var session = await LoginAsync(r.UserName, "Passw0rd!x");
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Skills", session.AccessToken));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Equipment", session.AccessToken));

            var before = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/Role/{r.RoleId}/permissions"));
            Assert.Equal(r.RoleName, before.GetProperty("roleName").GetString());
            Assert.False(before.GetProperty("isSystem").GetBoolean());
            Assert.Equal(1, before.GetProperty("grantedCount").GetInt32());
            Assert.Equal(Permissions.All.Count, before.GetProperty("totalCount").GetInt32());
            Assert.Equal(new[] { "Skills.View" }, PermissionsOf(before.GetProperty("groups")).Where(p => p.GetProperty("granted").GetBoolean()).Select(p => p.GetProperty("name").GetString()));

            // replace the whole list: Skills.View goes, two others come
            var put = await AsAdminAsync(HttpMethod.Put, $"/api/Role/{r.RoleId}/permissions", new { permissions = new[] { "Equipments.View", "FailureReports.View" } });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);
            var after = await ReadDataAsync(put);
            Assert.Equal(2, after.GetProperty("grantedCount").GetInt32());
            Assert.Equal(new[] { "Equipments.View", "FailureReports.View" },
                PermissionsOf(after.GetProperty("groups")).Where(p => p.GetProperty("granted").GetBoolean()).Select(p => p.GetProperty("name").GetString()).OrderBy(n => n));

            // the name-based read agrees
            var names = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/Role/{r.RoleName}/permissions"))).EnumerateArray().Select(e => e.GetString()).OrderBy(n => n);
            Assert.Equal(new[] { "Equipments.View", "FailureReports.View" }, names);

            // the user's old token is refused at once; after a refresh the new permissions apply
            Assert.Equal(HttpStatusCode.Unauthorized, await StatusOfAsync(HttpMethod.Get, "/api/Skills", session.AccessToken));
            var refreshed = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = session.RefreshToken }));
            var newToken = refreshed.GetProperty("accessToken").GetString()!;
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Skills", newToken));
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Equipment", newToken));
        }

        [Fact]
        public async Task Saving_the_same_permissions_again_changes_nothing_and_does_not_sign_the_users_out()
        {
            var r = await NewRoleWithUserAsync("Skills.View");
            var session = await LoginAsync(r.UserName, "Passw0rd!x");

            var put = await AsAdminAsync(HttpMethod.Put, $"/api/Role/{r.RoleId}/permissions", new { permissions = new[] { "Skills.View" } });
            Assert.Equal(HttpStatusCode.OK, put.StatusCode);

            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Skills", session.AccessToken));     // the token still works
        }

        [Fact]
        public async Task An_unknown_permission_an_unknown_role_and_a_missing_right_are_refused()
        {
            var r = await NewRoleWithUserAsync("Roles.View");
            var viewer = (await LoginAsync(r.UserName, "Passw0rd!x")).AccessToken;

            var unknownPermission = await AsAdminAsync(HttpMethod.Put, $"/api/Role/{r.RoleId}/permissions", new { permissions = new[] { "Equipments.View", "Nothing.Here" } });
            Assert.Equal(HttpStatusCode.BadRequest, unknownPermission.StatusCode);
            Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(unknownPermission)));

            var unknownRole = await AsAdminAsync(HttpMethod.Get, "/api/Role/999999999/permissions");
            Assert.Equal(HttpStatusCode.NotFound, unknownRole.StatusCode);
            Assert.Equal("role_not_found", ErrorCodeOf(await ReadJsonAsync(unknownRole)));
            Assert.Equal(HttpStatusCode.NotFound, (await AsAdminAsync(HttpMethod.Put, "/api/Role/999999999/permissions", new { permissions = Array.Empty<string>() })).StatusCode);

            // Roles.View reads, but only Roles.ManagePermissions writes
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, $"/api/Role/{r.RoleId}/permissions", viewer));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Put, $"/api/Role/{r.RoleId}/permissions", viewer, new { permissions = new[] { "Roles.ManagePermissions" } }));
        }

        // ------------------------------------------------------------------------------------------ the administrator role

        [Fact]
        public async Task The_administrator_role_keeps_every_permission_its_name_and_its_state_and_cannot_be_deleted()
        {
            var adminRoleId = await AdminRoleIdAsync();

            var view = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/Role/{adminRoleId}/permissions"));
            Assert.True(view.GetProperty("isSystem").GetBoolean());
            Assert.Equal(view.GetProperty("totalCount").GetInt32(), view.GetProperty("grantedCount").GetInt32());

            async Task AssertProtectedAsync(HttpResponseMessage response)
            {
                Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
                Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(response)));
            }

            await AssertProtectedAsync(await AsAdminAsync(HttpMethod.Put, $"/api/Role/{adminRoleId}/permissions", new { permissions = new[] { "Skills.View" } }));
            await AssertProtectedAsync(await AsAdminAsync(HttpMethod.Post, "/api/Role/AssignPermissions", new { roleName = "Admin", permissions = new[] { "Skills.View" } }));
            await AssertProtectedAsync(await AsAdminAsync(HttpMethod.Put, $"/api/Role/{adminRoleId}", new { name = "Boss", isActive = true }));
            await AssertProtectedAsync(await AsAdminAsync(HttpMethod.Put, $"/api/Role/{adminRoleId}", new { name = "Admin", isActive = false }));
            await AssertProtectedAsync(await AsAdminAsync(HttpMethod.Delete, $"/api/Role/{adminRoleId}"));

            // nothing was lost and the administrator still works
            var still = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/Role/{adminRoleId}/permissions"));
            Assert.Equal(still.GetProperty("totalCount").GetInt32(), still.GetProperty("grantedCount").GetInt32());

            // the list tells the UI which role is the system one
            var roles = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/Role?pageSize=100"))).EnumerateArray().ToList();
            Assert.Equal(new[] { "Admin" }, roles.Where(x => x.GetProperty("isSystem").GetBoolean()).Select(x => x.GetProperty("name").GetString()));
        }

        // ------------------------------------------------------------------------------------------ me

        [Fact]
        public async Task Me_returns_the_signed_in_user_with_his_role_and_current_permissions()
        {
            var r = await NewRoleWithUserAsync("Skills.View", "Equipments.View");
            var session = await LoginAsync(r.UserName, "Passw0rd!x");

            var me = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/Identity/me", bearer: session.AccessToken));
            Assert.Equal(r.UserId, me.GetProperty("userId").GetInt64());
            Assert.Equal(r.UserName, me.GetProperty("userName").GetString());
            Assert.False(string.IsNullOrWhiteSpace(me.GetProperty("fullName").GetString()));
            Assert.Equal(new[] { r.RoleName }, me.GetProperty("roles").EnumerateArray().Select(e => e.GetString()));
            Assert.Equal(new[] { "Equipments.View", "Skills.View" }, me.GetProperty("permissions").EnumerateArray().Select(e => e.GetString()));

            // after the role changes: the old token is refused, and "me" with the refreshed token shows the new permissions
            await AsAdminAsync(HttpMethod.Put, $"/api/Role/{r.RoleId}/permissions", new { permissions = new[] { "Users.View" } });
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/Identity/me", bearer: session.AccessToken)).StatusCode);
            var refreshed = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = session.RefreshToken }));
            var again = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/Identity/me", bearer: refreshed.GetProperty("accessToken").GetString()));
            Assert.Equal(new[] { "Users.View" }, again.GetProperty("permissions").EnumerateArray().Select(e => e.GetString()));

            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/Identity/me")).StatusCode);
        }
    }
}
