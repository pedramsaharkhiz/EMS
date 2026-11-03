using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The response envelope { success, message, data, meta, error, traceId } on the parts of the API that are not catalogs
    /// (users, roles, commands) and the conventions that go with it: 200 + null data for delete/commands, rowVersion on
    /// every PUT, UTC dates with a "Z".
    /// </summary>
    public class ApiEnvelopeTests : ApiTestBase
    {
        public ApiEnvelopeTests(EmsApiFactory factory) : base(factory) { }

        private static readonly HashSet<string> Members = new() { "success", "message", "data", "meta", "error", "traceId" };

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;

        private static void AssertSuccessEnvelope(JsonElement json)
        {
            Assert.Equal(Members, json.EnumerateObject().Select(p => p.Name).ToHashSet());
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, json.GetProperty("error").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        }

        [Fact]
        public async Task A_single_resource_has_all_six_members_with_the_resource_in_data_and_the_correlation_id_as_trace_id()
        {
            var admin = await AdminAsync();
            var corr = NewCorrelationId("it-env");
            var response = await SendAsync(HttpMethod.Get, "/api/Users", correlationId: corr, bearer: admin);
            var users = await ReadJsonAsync(response);
            var id = DataOf(users)[0].GetProperty("id").GetInt64();

            var one = await SendAsync(HttpMethod.Get, $"/api/Users/{id}", correlationId: corr, bearer: admin);
            Assert.Equal(HttpStatusCode.OK, one.StatusCode);
            var json = await ReadJsonAsync(one);
            AssertSuccessEnvelope(json);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("message").ValueKind);   // reads carry no message
            Assert.Equal(JsonValueKind.Null, json.GetProperty("meta").ValueKind);
            Assert.Equal(id, DataOf(json).GetProperty("id").GetInt64());
            Assert.Equal(corr, json.GetProperty("traceId").GetString());
            Assert.Equal(corr, one.Headers.GetValues("X-Correlation-ID").Single());
        }

        [Fact]
        public async Task The_user_list_is_an_array_in_data_with_the_paging_in_meta_pagination()
        {
            var json = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/Users?pageSize=1", bearer: await AdminAsync()));
            AssertSuccessEnvelope(json);
            Assert.Equal(JsonValueKind.Array, DataOf(json).ValueKind);
            Assert.Equal(1, PaginationOf(json).GetProperty("pageSize").GetInt32());
            Assert.True(PaginationOf(json).GetProperty("totalCount").GetInt32() >= 1);
        }

        [Fact]
        public async Task Small_unpaged_lists_are_an_array_in_data_without_pagination()
        {
            var admin = await AdminAsync();
            var json = await ReadJsonAsync(await SendAsync(HttpMethod.Get, "/api/Users/1/roles", bearer: admin));
            AssertSuccessEnvelope(json);
            Assert.Equal(JsonValueKind.Array, DataOf(json).ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("meta").ValueKind);
        }

        [Fact]
        public async Task Delete_and_commands_answer_200_with_a_message_and_null_data()
        {
            var admin = await AdminAsync();

            var brand = (await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Brand", new { title = "EnvDel", code = "ED" }, bearer: admin))).GetProperty("id").GetInt64();
            var delete = await SendAsync(HttpMethod.Delete, $"/api/Brand/{brand}", bearer: admin);
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
            var deleted = await ReadJsonAsync(delete);
            AssertSuccessEnvelope(deleted);
            Assert.Equal(JsonValueKind.Null, deleted.GetProperty("data").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(deleted.GetProperty("message").GetString()));

            var userId = await CreateUserAsync(admin, "env_cmd", "Passw0rd!x");
            var roleId = await CreateRoleAsync(admin, "EnvCmdRole");
            var assign = await SendAsync(HttpMethod.Post, $"/api/Users/{userId}/roles/{roleId}", bearer: admin);
            Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
            var done = await ReadJsonAsync(assign);
            AssertSuccessEnvelope(done);
            Assert.Equal(JsonValueKind.Null, done.GetProperty("data").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(done.GetProperty("message").GetString()));
        }

        [Fact]
        public async Task Create_is_201_with_a_location_and_a_message_for_users_and_roles_too()
        {
            var admin = await AdminAsync();

            var user = await SendAsync(HttpMethod.Post, "/api/Users",
                new { userName = "env_create", password = "Passw0rd!x", firstName = "E", lastName = "C", personnelCode = "PC-env_create" }, bearer: admin);
            Assert.Equal(HttpStatusCode.Created, user.StatusCode);
            var userJson = await ReadJsonAsync(user);
            AssertSuccessEnvelope(userJson);
            Assert.EndsWith($"/api/Users/{DataOf(userJson).GetProperty("id").GetInt64()}", user.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(userJson.GetProperty("message").GetString()));

            var role = await SendAsync(HttpMethod.Post, "/api/Role", new { name = "EnvCreateRole" }, bearer: admin);
            Assert.Equal(HttpStatusCode.Created, role.StatusCode);
            Assert.EndsWith($"/api/Role/{(await ReadDataAsync(role)).GetProperty("id").GetInt64()}", role.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------------------- rowVersion on users and roles

        [Fact]
        public async Task A_stale_user_row_version_is_409_a_current_one_works_and_no_version_still_works()
        {
            var admin = await AdminAsync();
            var id = await CreateUserAsync(admin, "env_ver_user", "Passw0rd!x");
            var v0 = (await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Users/{id}", bearer: admin))).GetProperty("rowVersion").GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(v0));

            object Body(string first, string? version) => version is null
                ? new { userName = "env_ver_user", firstName = first, lastName = "User", personnelCode = "PC-env_ver_user", isActive = true }
                : new { userName = "env_ver_user", firstName = first, lastName = "User", personnelCode = "PC-env_ver_user", isActive = true, rowVersion = version };

            var a = await SendAsync(HttpMethod.Put, $"/api/Users/{id}", Body("A", v0), bearer: admin);
            Assert.Equal(HttpStatusCode.OK, a.StatusCode);
            var v1 = (await ReadDataAsync(a)).GetProperty("rowVersion").GetString()!;
            Assert.NotEqual(v0, v1);

            var stale = await SendAsync(HttpMethod.Put, $"/api/Users/{id}", Body("B", v0), bearer: admin);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("concurrency_conflict", ErrorCodeOf(await ReadJsonAsync(stale)));

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Users/{id}", Body("C", v1), bearer: admin)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Users/{id}", Body("D", null), bearer: admin)).StatusCode);
        }

        [Fact]
        public async Task A_stale_role_row_version_is_409_a_current_one_works_and_no_version_still_works()
        {
            var admin = await AdminAsync();
            var id = await CreateRoleAsync(admin, "EnvVerRole");
            var v0 = (await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Role/{id}", bearer: admin))).GetProperty("rowVersion").GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(v0));

            var a = await SendAsync(HttpMethod.Put, $"/api/Role/{id}", new { name = "EnvVerRoleA", isActive = true, rowVersion = v0 }, bearer: admin);
            Assert.Equal(HttpStatusCode.OK, a.StatusCode);
            var v1 = (await ReadDataAsync(a)).GetProperty("rowVersion").GetString()!;
            Assert.NotEqual(v0, v1);

            var stale = await SendAsync(HttpMethod.Put, $"/api/Role/{id}", new { name = "EnvVerRoleB", isActive = true, rowVersion = v0 }, bearer: admin);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("concurrency_conflict", ErrorCodeOf(await ReadJsonAsync(stale)));

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Role/{id}", new { name = "EnvVerRoleC", isActive = true, rowVersion = v1 }, bearer: admin)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Put, $"/api/Role/{id}", new { name = "EnvVerRoleD", isActive = true }, bearer: admin)).StatusCode);
        }

        // ------------------------------------------------------------------------------- dates

        [Fact]
        public async Task Dates_are_iso_8601_in_utc_with_a_Z()
        {
            var admin = await AdminAsync();
            var id = await CreateUserAsync(admin, "env_dates", "Passw0rd!x");
            var user = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Users/{id}", bearer: admin));
            Assert.EndsWith("Z", user.GetProperty("createdDateTime").GetString());

            var login = await ReadDataAsync(await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "env_dates", password = "Passw0rd!x" }));
            Assert.EndsWith("Z", login.GetProperty("accessTokenExpiresAt").GetString());
            Assert.EndsWith("Z", login.GetProperty("refreshTokenExpiresAt").GetString());
        }
    }
}
