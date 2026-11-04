using EMS.Domain;
using EMS.Domain.Enums;
using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>ReportTypeCode (enum) and the ReportTypes table describe the same report types: row id = enum value and Code = enum name. Administrators manage the table, but the rows the code relies on are protected.</summary>
    public class ReportTypeSystemRowsTests : ApiTestBase
    {
        public ReportTypeSystemRowsTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        public static IEnumerable<object[]> SystemReportTypes =>
            Enum.GetValues<ReportTypeCode>().Where(c => c != ReportTypeCode.Unknown).Select(c => new object[] { c });

        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        [Fact]
        public void Every_enum_value_except_Unknown_has_a_title_and_Unknown_has_none()
        {
            foreach (var code in Enum.GetValues<ReportTypeCode>().Where(c => c != ReportTypeCode.Unknown))
                Assert.False(string.IsNullOrWhiteSpace(ReportTypeCatalog.Titles[code]), code + " has no title in ReportTypeCatalog");

            Assert.False(ReportTypeCatalog.Titles.ContainsKey(ReportTypeCode.Unknown));
            Assert.Equal(Enum.GetValues<ReportTypeCode>().Length - 1, ReportTypeCatalog.Titles.Count);
        }

        [Theory, MemberData(nameof(SystemReportTypes))]
        public async Task Each_enum_value_is_a_row_whose_id_is_the_value_and_whose_code_is_the_name(ReportTypeCode code)
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/ReportType/{(long)code}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);
            Assert.Equal((long)code, json.GetProperty("id").GetInt64());
            Assert.Equal(code.ToString(), json.GetProperty("code").GetString());
            Assert.True(json.GetProperty("isActive").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("title").GetString()));
        }

        [Theory, MemberData(nameof(SystemReportTypes))]
        public async Task A_system_row_cannot_be_deactivated_or_deleted(ReportTypeCode code)
        {
            var url = $"/api/ReportType/{(long)code}";

            var delete = await SendAdminAsync(HttpMethod.Delete, url);
            Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
            Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(delete)));

            var deactivate = await SendAdminAsync(HttpMethod.Put, url, new { isActive = false });
            Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
            Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(deactivate)));

            Assert.True((await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, url))).GetProperty("isActive").GetBoolean());
        }

        [Fact]
        public async Task A_system_row_keeps_its_code_but_its_title_can_be_edited()
        {
            var url = $"/api/ReportType/{(long)ReportTypeCode.MaintenanceNeed}";
            var original = (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, url))).GetProperty("title").GetString();

            var changeCode = await SendAdminAsync(HttpMethod.Put, url, new { code = "Gone", isActive = true });
            Assert.Equal(HttpStatusCode.Conflict, changeCode.StatusCode);
            Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(changeCode)));

            // sending the same code (a form that posts every field) is fine, and so is renaming
            var renamed = Uniq("sys");
            var ok = await SendAdminAsync(HttpMethod.Put, url, new { title = renamed, code = "MaintenanceNeed", isActive = true });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal(renamed, (await ReadDataAsync(ok)).GetProperty("title").GetString());

            // put the title back so other tests see the seeded value
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, url, new { title = original, isActive = true })).StatusCode);
        }

        [Fact]
        public async Task Other_rows_can_be_added_but_cannot_take_a_system_code()
        {
            var created = await SendAdminAsync(HttpMethod.Post, "/api/ReportType", new { title = Uniq("custom"), code = "CUSTOM_1" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await ReadDataAsync(created)).GetProperty("id").GetInt64();
            Assert.True(id > (long)Enum.GetValues<ReportTypeCode>().Max(), "a new row must not reuse an id reserved for the enum");

            foreach (var reserved in new[] { "Failure", "incident", " Complaint " })
            {
                var clash = await SendAdminAsync(HttpMethod.Post, "/api/ReportType", new { title = Uniq("clash"), code = reserved });
                Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
                Assert.Equal("reserved_code", ErrorCodeOf(await ReadJsonAsync(clash)));
            }

            var update = await SendAdminAsync(HttpMethod.Put, $"/api/ReportType/{id}", new { code = "Failure", isActive = true });
            Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
            Assert.Equal("reserved_code", ErrorCodeOf(await ReadJsonAsync(update)));

            // a custom row can still be deactivated normally
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/ReportType/{id}")).StatusCode);
        }
    }
}
