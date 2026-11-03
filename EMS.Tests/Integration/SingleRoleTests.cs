using Microsoft.EntityFrameworkCore;
using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>A user has at most one active role, on every path that assigns roles.</summary>
    public class SingleRoleTests : ApiTestBase
    {
        public SingleRoleTests(EmsApiFactory factory) : base(factory) { }

        private async Task<List<string>> RolesOfAsync(string admin, long userId) =>
            (await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Users/{userId}/roles", bearer: admin)))
                .EnumerateArray().Select(r => r.GetProperty("name").GetString()!).ToList();

        [Fact]
        public async Task Setting_more_than_one_role_is_rejected_with_a_persian_field_error()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            var a = await CreateRoleAsync(admin, "SrA");
            var b = await CreateRoleAsync(admin, "SrB");
            var user = await CreateUserAsync(admin, "sr_set", "Passw0rd!x");

            var response = await SendAsync(HttpMethod.Put, $"/api/Users/{user}/roles", new { roleIds = new[] { a, b } }, bearer: admin);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Contains("roleIds", ErrorFieldsOf(json));
            Assert.Contains("فقط یک نقش", json.GetProperty("error").GetProperty("details")[0].GetProperty("message").GetString());
            Assert.Empty(await RolesOfAsync(admin, user));
        }

        [Fact]
        public async Task Setting_a_role_replaces_the_previous_one_and_an_empty_list_removes_it()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            var a = await CreateRoleAsync(admin, "SrRepA", "Brands.View");
            var b = await CreateRoleAsync(admin, "SrRepB", "Skills.View");
            var user = await CreateUserAsync(admin, "sr_rep", "Passw0rd!x");

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Users/{user}/roles", new { roleIds = new[] { a } }, bearer: admin)).StatusCode);
            Assert.Equal(new[] { "SrRepA" }, await RolesOfAsync(admin, user));

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Users/{user}/roles", new { roleIds = new[] { b } }, bearer: admin)).StatusCode);
            Assert.Equal(new[] { "SrRepB" }, await RolesOfAsync(admin, user));

            var login = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "sr_rep", password = "Passw0rd!x" }));
            Assert.Equal(new[] { "Skills.View" }, login.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToArray());

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Users/{user}/roles", new { roleIds = Array.Empty<long>() }, bearer: admin)).StatusCode);
            Assert.Empty(await RolesOfAsync(admin, user));
        }

        [Fact]
        public async Task Adding_a_second_role_is_a_409_with_its_own_code()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            var a = await CreateRoleAsync(admin, "SrAddA");
            var b = await CreateRoleAsync(admin, "SrAddB");
            var user = await CreateUserAsync(admin, "sr_add", "Passw0rd!x");

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post, $"/api/Users/{user}/roles/{a}", bearer: admin)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post, $"/api/Users/{user}/roles/{a}", bearer: admin)).StatusCode);   // same role again: nothing to do

            var second = await SendAsync(HttpMethod.Post, $"/api/Users/{user}/roles/{b}", bearer: admin);
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            Assert.Equal("user_already_has_role", ErrorCodeOf(await ReadJsonAsync(second)));
            Assert.Equal(new[] { "SrAddA" }, await RolesOfAsync(admin, user));

            // after removing the first, the second can be assigned
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Users/{user}/roles/{a}", bearer: admin)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post, $"/api/Users/{user}/roles/{b}", bearer: admin)).StatusCode);
            Assert.Equal(new[] { "SrAddB" }, await RolesOfAsync(admin, user));
        }

        [Fact]
        public async Task Assigning_users_from_the_role_side_rejects_a_user_who_has_another_role()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            var a = await CreateRoleAsync(admin, "SrSideA");
            var b = await CreateRoleAsync(admin, "SrSideB");
            var holder = await CreateUserAsync(admin, "sr_holder", "Passw0rd!x");
            var free = await CreateUserAsync(admin, "sr_free", "Passw0rd!x");
            await AssignRoleAsync(admin, holder, a);

            var rejected = await SendAsync(HttpMethod.Put, $"/api/Role/{b}/users", new { userIds = new[] { holder, free } }, bearer: admin);
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            Assert.Equal("user_already_has_role", ErrorCodeOf(await ReadJsonAsync(rejected)));
            Assert.Empty(await RolesOfAsync(admin, free));

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Role/{b}/users", new { userIds = new[] { free } }, bearer: admin)).StatusCode);
            Assert.Equal(new[] { "SrSideB" }, await RolesOfAsync(admin, free));
        }

        [Fact]
        public async Task Creating_or_updating_a_user_accepts_at_most_one_role_id()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            var a = await CreateRoleAsync(admin, "SrUsrA");
            var b = await CreateRoleAsync(admin, "SrUsrB");

            var tooMany = await SendAsync(HttpMethod.Post, "/api/Users",
                new { userName = "sr_c1", password = "Passw0rd!x", firstName = "a", lastName = "b", personnelCode = "SRC1", roleIds = new[] { a, b } }, bearer: admin);
            Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
            Assert.Contains("roleIds", ErrorFieldsOf(await ReadJsonAsync(tooMany)));

            var created = await SendAsync(HttpMethod.Post, "/api/Users",
                new { userName = "sr_c2", password = "Passw0rd!x", firstName = "a", lastName = "b", personnelCode = "SRC2", roleIds = new[] { a } }, bearer: admin);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await ReadDataAsync(created)).GetProperty("id").GetInt64();

            var swap = await SendAsync(HttpMethod.Put, $"/api/Users/{id}",
                new { userName = "sr_c2", firstName = "a", lastName = "b", personnelCode = "SRC2", isActive = true, roleIds = new[] { b } }, bearer: admin);
            Assert.Equal(HttpStatusCode.OK, swap.StatusCode);
            Assert.Equal(new[] { "SrUsrB" }, await RolesOfAsync(admin, id));

            var twoOnUpdate = await SendAsync(HttpMethod.Put, $"/api/Users/{id}",
                new { userName = "sr_c2", firstName = "a", lastName = "b", personnelCode = "SRC2", isActive = true, roleIds = new[] { a, b } }, bearer: admin);
            Assert.Equal(HttpStatusCode.BadRequest, twoOnUpdate.StatusCode);
        }

        [Fact]
        public async Task The_database_model_has_a_unique_filtered_index_on_the_active_role_of_a_user()
        {
            using var db = EMS.Tests.Support.TestDb.SqlServerModelOnly();
            var model = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>(db).Model;
            var entity = model.GetEntityTypes().Single(t => t.GetTableName() == "AspNetUserRoles");
            var index = entity.GetIndexes().Single(i => i.GetDatabaseName() == "UX_AspNetUserRoles_UserId_Active");
            Assert.True(index.IsUnique);
            Assert.Equal("[IsActive] = 1", index.GetFilter());
            Assert.Equal(new[] { "UserId" }, index.Properties.Select(p => p.Name).ToArray());
            await Task.CompletedTask;
        }
    }
}
