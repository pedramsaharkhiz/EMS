using System.Net;

namespace EMS.Tests.Integration;

public class NoteCategoryStartupTests : ApiTestBase
{
    public NoteCategoryStartupTests(EmsApiFactory factory) : base(factory) { }

    [Fact]
    public async Task The_normal_application_startup_seeds_each_requested_note_subject()
    {
        var login = await LoginAdminAsync();
        foreach (var title in new[] { "پیگیری", "هشدار", "تذکر", "هماهنگی", "درخواست اطلاعات", "راهنمایی فنی" })
        {
            var response = await SendAsync(HttpMethod.Get, "/api/NoteCategory?title=" + Uri.EscapeDataString(title), bearer: login.AccessToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var item = Assert.Single((await ReadDataAsync(response)).EnumerateArray(), x => x.GetProperty("title").GetString() == title);
            Assert.True(item.GetProperty("isActive").GetBoolean());
        }
    }
}
