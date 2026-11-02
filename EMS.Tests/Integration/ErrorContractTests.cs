using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>Every kind of error the API can return is one RFC 7807 problem document with a stable code.</summary>
    public class ErrorContractTests : ApiTestBase
    {
        public ErrorContractTests(EmsApiFactory factory) : base(factory) { }

        private static readonly string[] RequiredMembers =
            { "type", "title", "status", "detail", "instance", "code", "message", "correlationId", "traceId" };

        private async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var json = await ReadJsonAsync(response);
            foreach (var member in RequiredMembers)
                Assert.True(json.TryGetProperty(member, out _), $"'{member}' is missing from {await Task.FromResult(json.ToString())}");

            Assert.Equal((int)status, json.GetProperty("status").GetInt32());
            Assert.Equal(code, json.GetProperty("code").GetString());
            Assert.Equal(json.GetProperty("detail").GetString(), json.GetProperty("message").GetString());
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("message").GetString()));
            Assert.Equal(response.Headers.GetValues("X-Correlation-ID").Single(), json.GetProperty("correlationId").GetString());
            Assert.StartsWith("https://", json.GetProperty("type").GetString());
            return json;
        }

        private async Task<string> AdminTokenAsync() => (await LoginAdminAsync()).AccessToken;

        [Fact]
        public async Task A_failed_login_is_a_401_problem_with_the_invalid_credentials_code()
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "nobody", password = "Wrong1pass" });
            var json = await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "invalid_credentials");
            Assert.Equal(GenericLoginError, json.GetProperty("message").GetString());
            Assert.Equal("/api/Identity/Login", json.GetProperty("instance").GetString());
        }

        [Fact]
        public async Task An_invalid_refresh_token_has_its_own_code()
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = "nope" });
            await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "invalid_refresh_token");
        }

        [Fact]
        public async Task Authentication_and_authorization_failures_produced_by_the_framework_also_carry_a_problem_body()
        {
            // no token: the JWT challenge writes no body of its own
            await AssertProblemAsync(await SendAsync(HttpMethod.Get, "/api/Users"), HttpStatusCode.Unauthorized, "unauthorized");

            // a token without the permission
            var admin = await AdminTokenAsync();
            await CreateUserAsync(admin, "ec_noperm", "Passw0rd!x");
            var plain = await LoginAsync("ec_noperm", "Passw0rd!x");
            await AssertProblemAsync(await SendAsync(HttpMethod.Get, "/api/Users", bearer: plain.AccessToken), HttpStatusCode.Forbidden, "forbidden");
        }

        [Fact]
        public async Task Unknown_routes_and_wrong_methods_are_problems_too()
        {
            await AssertProblemAsync(await SendAsync(HttpMethod.Get, "/api/this-does-not-exist"), HttpStatusCode.NotFound, "not_found");
            await AssertProblemAsync(await SendAsync(HttpMethod.Delete, "/api/Identity/Login"), HttpStatusCode.MethodNotAllowed, "method_not_allowed");
        }

        [Fact]
        public async Task A_missing_entity_is_a_404_problem_keeping_its_specific_message()
        {
            var response = await SendAsync(HttpMethod.Get, "/api/Brand/987654321", bearer: await AdminTokenAsync());
            var json = await AssertProblemAsync(response, HttpStatusCode.NotFound, "not_found");
            Assert.Contains("برند", json.GetProperty("message").GetString());
        }

        [Fact]
        public async Task Validation_failures_list_the_fields_as_a_dictionary_with_the_validation_failed_code()
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Brand", new { title = "", code = "X" }, bearer: await AdminTokenAsync());
            var json = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");

            Assert.Equal("One or more validation errors occurred.", json.GetProperty("title").GetString());
            var errors = json.GetProperty("errors");
            var title = errors.EnumerateObject().Single(p => p.Name.Equals("Title", StringComparison.OrdinalIgnoreCase)).Value;
            Assert.All(title.EnumerateArray(), m => Assert.False(string.IsNullOrWhiteSpace(m.GetString())));
        }

        [Fact]
        public async Task Malformed_json_is_a_400_problem_not_an_empty_response()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Brand")
            {
                Content = new StringContent("{ this is not json", Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await AdminTokenAsync());
            await AssertProblemAsync(await _client.SendAsync(request), HttpStatusCode.BadRequest, "validation_failed");
        }

        [Fact]
        public async Task Business_rule_conflicts_carry_specific_codes()
        {
            var admin = await AdminTokenAsync();

            // concurrency
            var created = await ReadJsonAsync(await SendAsync(HttpMethod.Post, "/api/Brand", new { title = "EcBrand", code = "E" }, bearer: admin));
            var id = created.GetProperty("brandId").GetInt64();
            await SendAsync(HttpMethod.Put, "/api/Brand", new { brandId = id, title = "EcBrand", code = "E2", isActive = true, rowVersion = "0" }, bearer: admin);
            await AssertProblemAsync(
                await SendAsync(HttpMethod.Put, "/api/Brand", new { brandId = id, title = "EcBrand", code = "E3", isActive = true, rowVersion = "0" }, bearer: admin),
                HttpStatusCode.Conflict, "concurrency_conflict");
            await AssertProblemAsync(
                await SendAsync(HttpMethod.Put, "/api/Brand", new { brandId = id, title = "EcBrand", code = "E4", isActive = true, rowVersion = "x" }, bearer: admin),
                HttpStatusCode.BadRequest, "invalid_row_version");

            // users and roles
            await CreateUserAsync(admin, "ec_dupuser", "Passw0rd!x");
            await AssertProblemAsync(
                await SendAsync(HttpMethod.Post, "/api/Users", new { userName = "ec_dupuser", password = "Passw0rd!x", firstName = "A", lastName = "B", personnelCode = "PC-other" }, bearer: admin),
                HttpStatusCode.Conflict, "user_name_taken");
            await CreateRoleAsync(admin, "EcRole");
            await AssertProblemAsync(await SendAsync(HttpMethod.Post, "/api/Role", new { name = "EcRole" }, bearer: admin), HttpStatusCode.Conflict, "role_name_taken");
        }

        [Fact]
        public async Task Catalog_rules_have_their_own_codes_duplicate_dependents_and_parents()
        {
            var admin = await AdminTokenAsync();

            // duplicate title
            await SendAsync(HttpMethod.Post, "/api/EquipmentCategory", new { title = "EcCatDup" }, bearer: admin);
            await AssertProblemAsync(await SendAsync(HttpMethod.Post, "/api/EquipmentCategory", new { title = "EcCatDup" }, bearer: admin),
                HttpStatusCode.Conflict, "duplicate_title");

            // active dependents
            var category = (await ReadJsonAsync(await SendAsync(HttpMethod.Post, "/api/EquipmentCategory", new { title = "EcCatDeps" }, bearer: admin))).GetProperty("equipmentCategoryId").GetInt64();
            var type = (await ReadJsonAsync(await SendAsync(HttpMethod.Post, "/api/EquipmentType", new { equipmentCategoryId = category, title = "EcTypeDeps", code = "T" }, bearer: admin))).GetProperty("equipmentTypeId").GetInt64();
            await AssertProblemAsync(await SendAsync(HttpMethod.Delete, $"/api/EquipmentCategory/{category}", bearer: admin),
                HttpStatusCode.Conflict, "has_active_dependents");

            // unknown parent / inactive parent
            await AssertProblemAsync(await SendAsync(HttpMethod.Post, "/api/EquipmentType", new { equipmentCategoryId = 987654321, title = "EcTypeNoParent", code = "T" }, bearer: admin),
                HttpStatusCode.NotFound, "parent_not_found");
            await SendAsync(HttpMethod.Delete, $"/api/EquipmentType/{type}", bearer: admin);
            await SendAsync(HttpMethod.Delete, $"/api/EquipmentCategory/{category}", bearer: admin);
            await AssertProblemAsync(await SendAsync(HttpMethod.Post, "/api/EquipmentType", new { equipmentCategoryId = category, title = "EcTypeInactiveParent", code = "T" }, bearer: admin),
                HttpStatusCode.BadRequest, "parent_inactive");
        }

        [Fact]
        public async Task The_correlation_id_in_the_body_is_the_one_the_client_sent()
        {
            var corr = NewCorrelationId("it-err");
            var response = await SendAsync(HttpMethod.Get, "/api/Users", correlationId: corr);
            var json = await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
            Assert.Equal(corr, json.GetProperty("correlationId").GetString());
            Assert.Equal(corr, json.GetProperty("traceId").GetString());
        }
    }
}
