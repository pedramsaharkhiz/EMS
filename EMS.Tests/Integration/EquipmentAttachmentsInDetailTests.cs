using EMS.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>GET /api/Equipment/{id} lists the equipment's active attachments; the list and every other answer carry attachmentCount.</summary>
    public class EquipmentAttachmentsInDetailTests : ApiTestBase
    {
        public EquipmentAttachmentsInDetailTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private static byte[] PdfBytes(int size = 500) => Encoding.ASCII.GetBytes("%PDF-1.7\n").Concat(Enumerable.Range(0, size).Select(i => (byte)(i % 200 + 32))).ToArray();

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private sealed record Fixture(long TypeId, long LocationTypeId);
        private async Task<Fixture> NewFixtureAsync()
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("adcat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("adtype"), code = "T" }, "id");
            var location = await CatalogFixtures.PostAsync(this, "/api/EquipmentLocationType", new { title = Uniq("adloc"), code = "L" }, "id");
            return new Fixture(type, location);
        }

        private async Task<long> NewEquipmentAsync(Fixture f)
        {
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", new
            {
                yaasCode = Uniq("ADY"), owner = 1, equipmentTypeId = f.TypeId, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentEquipmentLocationTypeId = f.LocationTypeId,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<long> UploadAsync(long equipmentId, string fileName, string? description = null)
        {
            using var form = new MultipartFormDataContent { { new ByteArrayContent(PdfBytes()), "file", fileName } };
            var url = $"/api/Attachments/Equipments/{equipmentId}" + (description is null ? "" : $"?description={Uri.EscapeDataString(description)}");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await AdminAsync());
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<JsonElement> DetailAsync(long id) => await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));

        [Fact]
        public async Task The_detail_lists_the_active_attachments_newest_first_with_their_metadata()
        {
            var id = await NewEquipmentAsync(await NewFixtureAsync());
            var first = await UploadAsync(id, "a.pdf", "اولی");
            var second = await UploadAsync(id, "b.pdf");

            var detail = await DetailAsync(id);
            Assert.Equal(2, detail.GetProperty("attachmentCount").GetInt32());
            var attachments = detail.GetProperty("attachments").EnumerateArray().ToList();
            Assert.Equal(new[] { second, first }, attachments.Select(a => a.GetProperty("id").GetInt64()).ToArray());      // newest first
            Assert.Equal("b.pdf", attachments[0].GetProperty("fileName").GetString());
            Assert.Equal("اولی", attachments[1].GetProperty("description").GetString());
            Assert.Equal("application/pdf", attachments[0].GetProperty("contentType").GetString());
            Assert.True(attachments[0].GetProperty("sizeBytes").GetInt64() > 0);
            Assert.Equal("Equipments", attachments[0].GetProperty("tableName").GetString());
            Assert.Equal(id, attachments[0].GetProperty("entityId").GetInt64());
            Assert.EndsWith("Z", attachments[0].GetProperty("createdDateTime").GetString());

            // the id works against the download endpoint
            var download = await SendAdminAsync(HttpMethod.Get, $"/api/Attachments/{second}/download");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        }

        [Fact]
        public async Task An_equipment_without_files_has_an_empty_list_and_a_count_of_zero()
        {
            var id = await NewEquipmentAsync(await NewFixtureAsync());
            var detail = await DetailAsync(id);
            Assert.Equal(0, detail.GetProperty("attachmentCount").GetInt32());
            Assert.Equal(0, detail.GetProperty("attachments").GetArrayLength());
        }

        [Fact]
        public async Task A_deactivated_attachment_is_not_listed_or_counted_and_a_deactivated_equipment_still_lists_its_files()
        {
            var id = await NewEquipmentAsync(await NewFixtureAsync());
            var keep = await UploadAsync(id, "keep.pdf");
            var remove = await UploadAsync(id, "remove.pdf");
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Attachments/{remove}")).StatusCode);

            var detail = await DetailAsync(id);
            Assert.Equal(1, detail.GetProperty("attachmentCount").GetInt32());
            Assert.Equal(new[] { keep }, detail.GetProperty("attachments").EnumerateArray().Select(a => a.GetProperty("id").GetInt64()).ToArray());

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);
            var inactive = await DetailAsync(id);
            Assert.False(inactive.GetProperty("isActive").GetBoolean());
            Assert.Equal(1, inactive.GetProperty("attachments").GetArrayLength());
        }

        [Fact]
        public async Task The_list_counts_attachments_per_equipment_without_listing_them()
        {
            var f = await NewFixtureAsync();
            var two = await NewEquipmentAsync(f);
            var none = await NewEquipmentAsync(f);
            var one = await NewEquipmentAsync(f);
            await UploadAsync(two, "a.pdf"); await UploadAsync(two, "b.pdf"); await UploadAsync(one, "c.pdf");

            var list = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment?pageSize=100"))).EnumerateArray().ToDictionary(e => e.GetProperty("id").GetInt64());
            Assert.Equal(2, list[two].GetProperty("attachmentCount").GetInt32());
            Assert.Equal(0, list[none].GetProperty("attachmentCount").GetInt32());
            Assert.Equal(1, list[one].GetProperty("attachmentCount").GetInt32());
            Assert.False(list[two].TryGetProperty("attachments", out _));                        // the files themselves are only in the detail
        }

        [Fact]
        public async Task Update_and_move_count_the_attachments_but_do_not_list_them_and_registration_with_files_does_both()
        {
            var f = await NewFixtureAsync();
            var id = await NewEquipmentAsync(f);
            await UploadAsync(id, "a.pdf");

            var detail = await DetailAsync(id);
            var update = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", new
            {
                yaasCode = detail.GetProperty("yaasCode").GetString(), owner = 1, equipmentTypeId = f.TypeId,
                equipmentStatusId = (long)EquipmentStatusCode.InStock, isActive = true, description = "x",
            });
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var updated = await ReadDataAsync(update);
            Assert.Equal(1, updated.GetProperty("attachmentCount").GetInt32());
            Assert.False(updated.TryGetProperty("attachments", out _));

            var move = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentEquipmentLocationTypeId = f.LocationTypeId, description = "m" });
            Assert.Equal(1, (await ReadDataAsync(move)).GetProperty("attachmentCount").GetInt32());

            // registration with files
            using var form = new MultipartFormDataContent
            {
                { new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(new { yaasCode = Uniq("ADR"), owner = 1, equipmentTypeId = f.TypeId, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentEquipmentLocationTypeId = f.LocationTypeId })), "equipment" },
                { new ByteArrayContent(PdfBytes()), "file", "r.pdf" },
            };
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/Equipment/with-attachments") { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await AdminAsync());
            var registered = await ReadDataAsync(await _client.SendAsync(request));
            Assert.Equal(1, registered.GetProperty("attachmentCount").GetInt32());
            Assert.Equal(1, registered.GetProperty("attachments").GetArrayLength());

            // and what GET returns for it is the same shape
            var again = await DetailAsync(registered.GetProperty("id").GetInt64());
            Assert.Equal(1, again.GetProperty("attachments").GetArrayLength());
        }

        [Fact]
        public async Task A_user_who_may_view_equipment_sees_the_attachments_too()
        {
            var admin = await AdminAsync();
            var id = await NewEquipmentAsync(await NewFixtureAsync());
            await UploadAsync(id, "a.pdf");

            var role = await CreateRoleAsync(admin, "AdView" + Guid.NewGuid().ToString("N")[..4], "Equipments.View");
            var userName = "adview" + Guid.NewGuid().ToString("N")[..6];
            var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
            await AssignRoleAsync(admin, userId, role);
            var token = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            var detail = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Equipment/{id}", bearer: token));
            Assert.Equal(1, detail.GetProperty("attachments").GetArrayLength());

            var plainName = "adplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, plainName, "Passw0rd!x");
            var plain = (await LoginAsync(plainName, "Passw0rd!x")).AccessToken;
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"/api/Equipment/{id}", bearer: plain)).StatusCode);
        }
    }
}
