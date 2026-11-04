using EMS.Domain;
using EMS.Domain.Enums;
using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// PostCode (enum) and the Posts table describe the same posts: row id = enum value and
    /// Code = enum name. Administrators manage the table, but the rows the code relies on are protected.
    /// </summary>
    public class PostSystemRowsTests : ApiTestBase
    {
        public PostSystemRowsTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        public static IEnumerable<object[]> SystemPosts =>
            Enum.GetValues<PostCode>().Where(c => c != PostCode.Unknown).Select(c => new object[] { c });

        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        [Fact]
        public void Every_enum_value_except_Unknown_has_a_title_and_Unknown_has_none()
        {
            foreach (var code in Enum.GetValues<PostCode>().Where(c => c != PostCode.Unknown))
                Assert.False(string.IsNullOrWhiteSpace(PostCatalog.Titles[code]), code + " has no title in PostCatalog");

            Assert.False(PostCatalog.Titles.ContainsKey(PostCode.Unknown));
            Assert.Equal(Enum.GetValues<PostCode>().Length - 1, PostCatalog.Titles.Count);
        }

        [Theory, MemberData(nameof(SystemPosts))]
        public async Task Each_enum_value_is_a_row_whose_id_is_the_value_and_whose_code_is_the_name(PostCode code)
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Post/{(long)code}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);
            Assert.Equal((long)code, json.GetProperty("id").GetInt64());
            Assert.Equal(code.ToString(), json.GetProperty("code").GetString());
            Assert.True(json.GetProperty("isActive").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("title").GetString()));
        }

        [Theory, MemberData(nameof(SystemPosts))]
        public async Task A_system_post_cannot_be_deactivated_or_deleted(PostCode code)
        {
            var url = $"/api/Post/{(long)code}";

            var delete = await SendAdminAsync(HttpMethod.Delete, url);
            Assert.Equal(HttpStatusCode.Conflict, delete.StatusCode);
            Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(delete)));

            var deactivate = await SendAdminAsync(HttpMethod.Put, url, new { isActive = false });
            Assert.Equal(HttpStatusCode.Conflict, deactivate.StatusCode);
            Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(deactivate)));

            Assert.True((await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, url))).GetProperty("isActive").GetBoolean());
        }

        [Fact]
        public async Task A_system_post_keeps_its_code_but_its_title_can_be_edited()
        {
            var url = $"/api/Post/{(long)PostCode.UnitManager}";
            var original = (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, url))).GetProperty("title").GetString();

            var changeCode = await SendAdminAsync(HttpMethod.Put, url, new { code = "Gone", isActive = true });
            Assert.Equal(HttpStatusCode.Conflict, changeCode.StatusCode);
            Assert.Equal("protected_record", ErrorCodeOf(await ReadJsonAsync(changeCode)));

            // sending the same code (a form that posts every field) is fine, and so is renaming
            var renamed = Uniq("mgr");
            var ok = await SendAdminAsync(HttpMethod.Put, url, new { title = renamed, code = "UnitManager", isActive = true });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal(renamed, (await ReadDataAsync(ok)).GetProperty("title").GetString());

            // put the title back so other tests see the seeded value
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, url, new { title = original, isActive = true })).StatusCode);
        }

        [Fact]
        public async Task Other_posts_can_be_added_but_cannot_take_a_system_code()
        {
            var created = await SendAdminAsync(HttpMethod.Post, "/api/Post", new { title = Uniq("custom"), code = "CUSTOM_1" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await ReadDataAsync(created)).GetProperty("id").GetInt64();
            Assert.True(id > (long)Enum.GetValues<PostCode>().Max(), "a new post must not reuse an id reserved for the enum");

            foreach (var reserved in new[] { "Technician", "registrar", " UnitManager " })
            {
                var clash = await SendAdminAsync(HttpMethod.Post, "/api/Post", new { title = Uniq("clash"), code = reserved });
                Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
                Assert.Equal("reserved_code", ErrorCodeOf(await ReadJsonAsync(clash)));
            }

            var update = await SendAdminAsync(HttpMethod.Put, $"/api/Post/{id}", new { code = "Technician", isActive = true });
            Assert.Equal(HttpStatusCode.Conflict, update.StatusCode);
            Assert.Equal("reserved_code", ErrorCodeOf(await ReadJsonAsync(update)));

            // a custom post can still be deactivated normally
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Post/{id}")).StatusCode);
        }
    }
}
