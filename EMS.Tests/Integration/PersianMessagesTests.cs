using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EMS.Tests.Integration
{
    /// <summary>Every message the API sends to the UI (message and error.details) is Persian, also for framework-level failures.</summary>
    public class PersianMessagesTests : ApiTestBase
    {
        public PersianMessagesTests(EmsApiFactory factory) : base(factory) { }

        private static readonly Regex Persian = new(@"[؀-ۿ]");
        // words that legitimately appear inside Persian sentences
        private static readonly Regex Latin = new(@"\b(?!asc\b|desc\b|role\b|skill\b|department\b|post\b|isActive\b|code\b|hasTechnician\b|rowVersion\b|application\b|json\b|Content\b|Type\b)[A-Za-z]{3,}\b");

        private async Task AssertPersianAsync(string label, HttpResponseMessage response, HttpStatusCode status)
        {
            Assert.True(status == response.StatusCode, $"{label}: expected {(int)status} but got {(int)response.StatusCode}");
            var json = await ReadJsonAsync(response);
            Assert.False(json.GetProperty("success").GetBoolean(), label);
            var texts = new List<string> { json.GetProperty("message").GetString() ?? "" };
            texts.AddRange(json.GetProperty("error").GetProperty("details").EnumerateArray().Select(d => d.GetProperty("message").GetString() ?? ""));
            foreach (var text in texts)
            {
                Assert.True(Persian.IsMatch(text), $"{label}: '{text}' has no Persian text");
                Assert.False(Latin.IsMatch(Regex.Replace(text, "«[^»]*»", "")), $"{label}: '{text}' contains English words");   // «…» quotes the client's own input / a field name
            }
        }

        private async Task<HttpResponseMessage> RawAsync(HttpMethod method, string url, string? body, string contentType = "application/json")
        {
            var request = new HttpRequestMessage(method, url);
            if (body is not null) request.Content = new StringContent(body, Encoding.UTF8, contentType);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", (await LoginAdminAsync()).AccessToken);
            return await _client.SendAsync(request);
        }

        [Fact]
        public async Task User_validation_messages_are_persian()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            await AssertPersianAsync("create empty", await SendAsync(HttpMethod.Post, "/api/Users", new { }, bearer: admin), HttpStatusCode.BadRequest);
            await AssertPersianAsync("create long", await SendAsync(HttpMethod.Post, "/api/Users",
                new { userName = new string('u', 30), password = "Passw0rd!x", firstName = new string('f', 150), lastName = "", personnelCode = "PC", email = "not-an-email", nationalCode = new string('1', 20), phoneNumber = new string('1', 30) }, bearer: admin), HttpStatusCode.BadRequest);
            await AssertPersianAsync("update empty", await SendAsync(HttpMethod.Put, "/api/Users/1", new { }, bearer: admin), HttpStatusCode.BadRequest);
            await AssertPersianAsync("short password", await SendAsync(HttpMethod.Put, "/api/Users/1/password", new { newPassword = "abc" }, bearer: admin), HttpStatusCode.BadRequest);
            await AssertPersianAsync("empty password", await SendAsync(HttpMethod.Put, "/api/Users/1/password", new { newPassword = "" }, bearer: admin), HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Identity_errors_are_persian()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            foreach (var name in new[] { "a b", "کاربر" })
                await AssertPersianAsync("username " + name, await SendAsync(HttpMethod.Post, "/api/Users",
                    new { userName = name, password = "Passw0rd!x", firstName = "A", lastName = "B", personnelCode = "PC-" + name.Length }, bearer: admin), HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_and_refresh_validation_have_only_persian_messages()
        {
            await AssertPersianAsync("login empty", await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { }), HttpStatusCode.BadRequest);
            await AssertPersianAsync("refresh empty", await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { }), HttpStatusCode.BadRequest);
            var login = await ReadJsonAsync(await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { }));
            Assert.Equal(2, login.GetProperty("error").GetProperty("details").GetArrayLength());   // one message per field, no English duplicate
        }

        [Fact]
        public async Task Model_binding_failures_are_persian_and_do_not_leak_internals()
        {
            await AssertPersianAsync("empty body", await RawAsync(HttpMethod.Post, "/api/Brand", ""), HttpStatusCode.BadRequest);
            var malformed = await RawAsync(HttpMethod.Post, "/api/Brand", "{ not json");
            await AssertPersianAsync("malformed", malformed, HttpStatusCode.BadRequest);
            var text = await malformed.Content.ReadAsStringAsync();
            Assert.DoesNotContain("LineNumber", text);
            Assert.DoesNotContain("BytePositionInLine", text);
            await AssertPersianAsync("title is a number", await RawAsync(HttpMethod.Post, "/api/Brand", "{\"title\": 5}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("id of wrong type", await RawAsync(HttpMethod.Post, "/api/EquipmentType", "{\"equipmentCategoryId\":\"abc\",\"title\":\"t\"}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("pageSize=abc", await RawAsync(HttpMethod.Get, "/api/Brand?pageSize=abc", null), HttpStatusCode.BadRequest);
            await AssertPersianAsync("isActive=maybe", await RawAsync(HttpMethod.Get, "/api/Brand?isActive=maybe", null), HttpStatusCode.BadRequest);
            await AssertPersianAsync("brandId=abc", await RawAsync(HttpMethod.Get, "/api/EquipmentModel?brandId=abc", null), HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Skill_and_user_group_validation_messages_are_persian_with_fields()
        {
            await AssertPersianAsync("skills list", await RawAsync(HttpMethod.Get, "/api/Skills?pageSize=0&pageNumber=0&id=0&technicianUserId=0&minTechnicianCount=-1&title=" + new string('a', 200), null), HttpStatusCode.BadRequest);
            await AssertPersianAsync("skills groups", await RawAsync(HttpMethod.Get, "/api/Skills/groups?pageSize=0", null), HttpStatusCode.BadRequest);
            await AssertPersianAsync("skill id 0", await RawAsync(HttpMethod.Put, "/api/Skills/0", "{\"title\":\"x\"}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("user ids 0", await RawAsync(HttpMethod.Post, "/api/Users", "{\"userName\":\"nx\",\"password\":\"Passw0rd!x\",\"firstName\":\"a\",\"lastName\":\"b\",\"personnelCode\":\"P9\",\"roleIds\":[0],\"skillIds\":[0]}"), HttpStatusCode.BadRequest);
            var unknown = await RawAsync(HttpMethod.Post, "/api/Users", "{\"userName\":\"ny\",\"password\":\"Passw0rd!x\",\"firstName\":\"a\",\"lastName\":\"b\",\"personnelCode\":\"P8\",\"skillIds\":[99999]}");
            await AssertPersianAsync("unknown skill", unknown, HttpStatusCode.BadRequest);
            Assert.Equal("skills_invalid", ErrorCodeOf(await ReadJsonAsync(unknown)));
        }

        [Fact]
        public async Task Equipment_bulk_create_messages_are_persian()
        {
            var longText = new string('x', 600);
            await AssertPersianAsync("bulk empty", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk", "{}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk null item", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk", "{\"items\":[null]}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk bad fields", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk",
                "{\"items\":[{\"yaasCode\":\"\",\"serialNumber\":\"" + longText + "\",\"equipmentStatusId\":0,\"currentEquipmentLocationTypeId\":0,\"technicianUserId\":-1}]}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk duplicate", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk",
                "{\"items\":[{\"yaasCode\":\"DUP\",\"equipmentTypeId\":1,\"equipmentStatusId\":1,\"currentEquipmentLocationTypeId\":1},{\"yaasCode\":\"dup\",\"equipmentTypeId\":1,\"equipmentStatusId\":1,\"currentEquipmentLocationTypeId\":1}]}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk wrong json type", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk", "{\"items\":[{\"equipmentStatusId\":\"abc\"}]}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk not json", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk", "{\"items\":"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk items not array", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk", "{\"items\":5}"), HttpStatusCode.BadRequest);
            await AssertPersianAsync("bulk too many", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk",
                "{\"items\":[" + string.Join(",", Enumerable.Repeat("{\"yaasCode\":\"a\"}", 201)) + "]}"), HttpStatusCode.BadRequest);
            // business errors (references that do not exist, inactive technician ...)
            await AssertPersianAsync("bulk unknown refs", await RawAsync(HttpMethod.Post, "/api/Equipment/bulk",
                "{\"items\":[{\"yaasCode\":\"PM-1\",\"equipmentTypeId\":99999999,\"equipmentStatusId\":99999999,\"currentEquipmentLocationTypeId\":99999999,\"currentOrganizationId\":99999999,\"currentSupportingDepartmentId\":99999999,\"technicianUserId\":99999999}]}"), HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task A_non_json_body_is_a_415_in_the_error_envelope_in_persian()
        {
            var response = await RawAsync(HttpMethod.Post, "/api/Brand", "hello", "text/plain");
            await AssertPersianAsync("415", response, HttpStatusCode.UnsupportedMediaType);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal("unsupported_media_type", ErrorCodeOf(await ReadJsonAsync(response)));
        }

        [Fact]
        public async Task Assigning_an_unknown_permission_is_rejected_in_persian()
        {
            var admin = (await LoginAdminAsync()).AccessToken;
            await CreateRoleAsync(admin, "PmRole", "Skills.View");
            var response = await SendAsync(HttpMethod.Post, "/api/Role/AssignPermissions", new { roleName = "PmRole", permissions = new[] { "Nope.Nothing" } }, bearer: admin);
            await AssertPersianAsync("unknown permission", response, HttpStatusCode.BadRequest);
            Assert.Contains("permissions[0]", (await ReadJsonAsync(response)).GetProperty("error").GetProperty("details")[0].GetProperty("field").GetString());
        }
    }
}
