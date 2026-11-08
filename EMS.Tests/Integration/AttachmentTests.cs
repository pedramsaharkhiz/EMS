using EMS.Application.Services.Attachment;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Attachments of any [Attachable] entity (equipment here): the Tables registry seeded from the attribute, upload as a stream,
    /// limits and type checks, download, soft delete and the entity's own permissions. (The in-memory database cannot run the raw SQL of the
    /// SQL blob store; these tests use InMemoryAttachmentBlobStore, the SQL path has its own manual test.)
    /// </summary>
    public class AttachmentTests : ApiTestBase
    {
        public AttachmentTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private static readonly byte[] PngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(Enumerable.Range(0, 2000).Select(i => (byte)(i % 251))).ToArray();
        private static byte[] PdfBytes(int size = 3000) => Encoding.ASCII.GetBytes("%PDF-1.7\n").Concat(Enumerable.Range(0, size).Select(i => (byte)(i % 200 + 32))).ToArray();

        private async Task<long> NewEquipmentAsync()
        {
            var admin = await AdminAsync();
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("atcat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("attype"), code = "T" }, "id");
            var location = await CatalogFixtures.NewDepartmentAsync(this);
            var created = await SendAsync(HttpMethod.Post, "/api/Equipment", new
            {
                yaasCode = Uniq("AT"),
                owner = 1,
                equipmentTypeId = type,
                equipmentStatusId = (long)EMS.Domain.Enums.EquipmentStatusCode.InStock,
                currentSupportingDepartmentId = location
            }, bearer: admin);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            return (await ReadDataAsync(created)).GetProperty("id").GetInt64();
        }

        private async Task<HttpResponseMessage> UploadAsync(string bearer, long equipmentId, string fileName, byte[] bytes, string? description = null, string table = "Equipments")
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");   // what the client claims is irrelevant
            form.Add(file, "file", fileName);
            var url = $"/api/Attachments/{table}/{equipmentId}" + (description is null ? "" : $"?description={Uri.EscapeDataString(description)}");
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _client.SendAsync(request);
        }

        private async Task<JsonElement> UploadOkAsync(long equipmentId, string fileName, byte[] bytes, string? description = null)
        {
            var response = await UploadAsync(await AdminAsync(), equipmentId, fileName, bytes, description);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"upload -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadDataAsync(response);
        }

        private async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.False(json.GetProperty("success").GetBoolean());
            Assert.Equal(code, ErrorCodeOf(json));
            Assert.Matches("[؀-ۿ]", json.GetProperty("message").GetString()!);
        }

        // ------------------------------------------------------------------------------- the Tables registry

        [Fact]
        public async Task The_tables_registry_is_filled_from_the_attachable_attribute()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var row = await db.Tables.SingleAsync(t => t.TableName == "Equipments");
            Assert.True(row.IsAttachmentEnabled);
            Assert.Equal("تجهیز", row.DisplayName);
            Assert.Equal(1, await db.Tables.CountAsync(t => t.TableName == "Equipments"));   // seeding twice does not duplicate
        }

        [Fact]
        public async Task The_seeder_adds_changes_and_switches_off_without_deleting()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var seeder = scope.ServiceProvider.GetRequiredService<EMS.Infrastructure.Persistence.Seed.IAttachmentTableSeeder>();

            // a renamed display name and a disabled flag are repaired
            var equipments = await db.Tables.SingleAsync(t => t.TableName == "Equipments");
            equipments.DisplayName = "x"; equipments.IsAttachmentEnabled = false;
            // a table that is not attachable (any more) is switched off but kept
            db.Tables.Add(new Table { TableName = "Legacy_" + Guid.NewGuid().ToString("N")[..8], DisplayName = "قدیمی", IsAttachmentEnabled = true });
            await db.SaveChangesAsync();

            var (added, changed) = await seeder.SeedAsync();
            Assert.Equal(0, added);
            Assert.Equal(2, changed);

            db.ChangeTracker.Clear();
            Assert.Equal("تجهیز", (await db.Tables.SingleAsync(t => t.TableName == "Equipments")).DisplayName);
            Assert.True((await db.Tables.SingleAsync(t => t.TableName == "Equipments")).IsAttachmentEnabled);
            Assert.DoesNotContain(await db.Tables.Where(t => t.TableName.StartsWith("Legacy_")).ToListAsync(), t => t.IsAttachmentEnabled);
            Assert.Equal((0, 0), await seeder.SeedAsync());   // idempotent
        }

        // ------------------------------------------------------------------------------- upload, list, download

        [Fact]
        public async Task Upload_returns_201_with_the_file_metadata_and_the_bytes_are_stored_as_sent()
        {
            var id = await NewEquipmentAsync();
            var response = await UploadAsync(await AdminAsync(), id, "نقشه تجهیز.png", PngBytes, "  نمای کلی  ");

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var envelope = await ReadJsonAsync(response);
            Assert.True(envelope.GetProperty("success").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(envelope.GetProperty("message").GetString()));
            var item = DataOf(envelope);
            Assert.Equal("Equipments", item.GetProperty("tableName").GetString());
            Assert.Equal(id, item.GetProperty("entityId").GetInt64());
            Assert.Equal("نقشه تجهیز.png", item.GetProperty("fileName").GetString());
            Assert.Equal("image/png", item.GetProperty("contentType").GetString());            // from the extension, not from the client
            Assert.Equal(PngBytes.Length, item.GetProperty("sizeBytes").GetInt64());
            Assert.Equal("نمای کلی", item.GetProperty("description").GetString());
            Assert.True(item.GetProperty("isActive").GetBoolean());
            Assert.EndsWith($"/api/Attachments/{item.GetProperty("id").GetInt64()}/download", response.Headers.Location!.AbsolutePath);

            var streamId = await StreamIdOfAsync(item.GetProperty("id").GetInt64());
            Assert.Equal(PngBytes, InMemoryAttachmentBlobStore.Files[streamId]);
        }

        private async Task<Guid> StreamIdOfAsync(long attachmentId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            return (await db.EntityAttachments.AsNoTracking().SingleAsync(a => a.EntityAttachmentId == attachmentId)).StreamId!.Value;
        }

        [Fact]
        public async Task Download_returns_the_exact_bytes_with_the_stored_type_and_the_file_name_as_an_attachment()
        {
            var id = await NewEquipmentAsync();
            var pdf = PdfBytes();
            var item = await UploadOkAsync(id, "گزارش فنی.pdf", pdf);

            var response = await SendAsync(HttpMethod.Get, $"/api/Attachments/{item.GetProperty("id").GetInt64()}/download", bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal("attachment", response.Content.Headers.ContentDisposition!.DispositionType);
            Assert.Equal("گزارش فنی.pdf", response.Content.Headers.ContentDisposition.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName!.Trim('"'));
            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.Equal(pdf, bytes);
            Assert.Equal(SHA256.HashData(pdf), SHA256.HashData(bytes));
        }

        [Fact]
        public async Task The_list_is_paged_newest_first_and_only_shows_the_records_own_active_files()
        {
            var one = await NewEquipmentAsync();
            var other = await NewEquipmentAsync();
            var a = (await UploadOkAsync(one, "a.pdf", PdfBytes())).GetProperty("id").GetInt64();
            var b = (await UploadOkAsync(one, "b.png", PngBytes)).GetProperty("id").GetInt64();
            await UploadOkAsync(other, "x.txt", Encoding.UTF8.GetBytes("سلام دنیا"));

            var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{one}", bearer: await AdminAsync()));
            Assert.Equal(new[] { b, a }, DataOf(list).EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToArray());
            Assert.Equal(2, PaginationOf(list).GetProperty("totalCount").GetInt32());

            var paged = await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{one}?pageSize=1&pageNumber=2", bearer: await AdminAsync()));
            Assert.Equal(new[] { a }, DataOf(paged).EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToArray());
        }

        // ------------------------------------------------------------------------------- what is refused

        [Fact]
        public async Task A_file_over_the_size_limit_is_refused_and_nothing_is_left_behind()
        {
            var id = await NewEquipmentAsync();
            var tooBig = PdfBytes((int)AttachmentRules.MaxFileSizeBytes + 10);

            await AssertErrorAsync(await UploadAsync(await AdminAsync(), id, "big.pdf", tooBig), HttpStatusCode.BadRequest, "attachment_too_large");

            // no attachment row exists. (The file row of the aborted upload is rolled back with the transaction on SQL Server; the in-memory
            // database used here has no transactions, so only the attachment itself can be checked.)
            Assert.Equal(0, (await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}", bearer: await AdminAsync()))).GetProperty("data").GetArrayLength());
        }

        [Fact]
        public async Task A_file_exactly_at_the_limit_is_accepted()
        {
            var id = await NewEquipmentAsync();
            var item = await UploadOkAsync(id, "max.pdf", PdfBytes((int)AttachmentRules.MaxFileSizeBytes - 9));
            Assert.Equal(AttachmentRules.MaxFileSizeBytes, item.GetProperty("sizeBytes").GetInt64());
        }

        [Theory]
        [InlineData("virus.exe")]
        [InlineData("page.html")]
        [InlineData("image.svg")]
        [InlineData("noextension")]
        [InlineData("double.pdf.exe")]
        public async Task A_type_that_is_not_allowed_is_refused(string fileName)
        {
            var id = await NewEquipmentAsync();
            await AssertErrorAsync(await UploadAsync(await AdminAsync(), id, fileName, PdfBytes()), HttpStatusCode.BadRequest, "attachment_type_not_allowed");
        }

        [Fact]
        public async Task A_renamed_file_is_refused_because_its_content_does_not_match_the_extension()
        {
            var id = await NewEquipmentAsync();
            var exe = Encoding.ASCII.GetBytes("MZ\u0090\u0000\u0003\u0000\u0000\u0000").Concat(new byte[500]).ToArray();
            await AssertErrorAsync(await UploadAsync(await AdminAsync(), id, "report.pdf", exe), HttpStatusCode.BadRequest, "attachment_content_mismatch");
            await AssertErrorAsync(await UploadAsync(await AdminAsync(), id, "photo.png", PdfBytes()), HttpStatusCode.BadRequest, "attachment_content_mismatch");
            await AssertErrorAsync(await UploadAsync(await AdminAsync(), id, "notes.txt", new byte[] { 0x68, 0x00, 0x69, 0x00 }), HttpStatusCode.BadRequest, "attachment_content_mismatch");
        }

        [Fact]
        public async Task An_empty_file_a_missing_file_and_a_non_multipart_body_are_refused()
        {
            var id = await NewEquipmentAsync();
            var admin = await AdminAsync();
            await AssertErrorAsync(await UploadAsync(admin, id, "empty.pdf", Array.Empty<byte>()), HttpStatusCode.BadRequest, "attachment_file_required");

            using var onlyField = new MultipartFormDataContent { { new StringContent("x"), "note" } };
            var noFile = new HttpRequestMessage(HttpMethod.Post, $"/api/Attachments/Equipments/{id}") { Content = onlyField };
            noFile.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            await AssertErrorAsync(await _client.SendAsync(noFile), HttpStatusCode.BadRequest, "attachment_file_required");

            var json = new HttpRequestMessage(HttpMethod.Post, $"/api/Attachments/Equipments/{id}") { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            json.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            await AssertErrorAsync(await _client.SendAsync(json), HttpStatusCode.UnsupportedMediaType, "unsupported_media_type");
        }

        [Fact]
        public async Task A_dangerous_file_name_is_cleaned_and_a_long_description_is_a_field_error()
        {
            var id = await NewEquipmentAsync();
            var item = await UploadOkAsync(id, @"..\..\windows\system32\report?.pdf", PdfBytes());
            Assert.Equal("report.pdf", item.GetProperty("fileName").GetString());

            var response = await UploadAsync(await AdminAsync(), id, "a.pdf", PdfBytes(), new string('d', 501));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("description", ErrorFieldsOf(await ReadJsonAsync(response)));
        }

        [Fact]
        public async Task A_record_can_have_at_most_twenty_active_attachments_and_a_deactivated_one_frees_a_place()
        {
            var id = await NewEquipmentAsync();
            long first = 0;
            for (var i = 0; i < AttachmentRules.MaxActivePerEntity; i++)
            {
                var item = await UploadOkAsync(id, $"f{i}.txt", Encoding.UTF8.GetBytes("x" + i));
                if (i == 0) first = item.GetProperty("id").GetInt64();
            }

            await AssertErrorAsync(await UploadAsync(await AdminAsync(), id, "extra.txt", Encoding.UTF8.GetBytes("x")), HttpStatusCode.Conflict, "attachment_limit_reached");

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Attachments/{first}", bearer: await AdminAsync())).StatusCode);
            await UploadOkAsync(id, "extra.txt", Encoding.UTF8.GetBytes("x"));
        }

        [Fact]
        public async Task Unknown_or_disabled_tables_and_missing_records_are_404()
        {
            var admin = await AdminAsync();
            var id = await NewEquipmentAsync();

            await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/NoSuchTable/{id}", bearer: admin), HttpStatusCode.NotFound, "attachment_target_not_found");
            await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Brands/{id}", bearer: admin), HttpStatusCode.NotFound, "attachment_target_not_found");   // exists, but not attachable
            await AssertErrorAsync(await SendAsync(HttpMethod.Get, "/api/Attachments/Equipments/99999999", bearer: admin), HttpStatusCode.NotFound, "attachment_target_not_found");
            await AssertErrorAsync(await UploadAsync(admin, 99999999, "a.pdf", PdfBytes()), HttpStatusCode.NotFound, "attachment_target_not_found");
            await AssertErrorAsync(await SendAsync(HttpMethod.Get, "/api/Attachments/99999999/download", bearer: admin), HttpStatusCode.NotFound, "not_found");
            await AssertErrorAsync(await SendAsync(HttpMethod.Delete, "/api/Attachments/99999999", bearer: admin), HttpStatusCode.NotFound, "not_found");

            // a deactivated equipment no longer takes attachments
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Equipment/{id}", bearer: admin)).StatusCode);
            await AssertErrorAsync(await UploadAsync(admin, id, "a.pdf", PdfBytes()), HttpStatusCode.NotFound, "attachment_target_not_found");
        }

        // ------------------------------------------------------------------------------- delete (soft)

        [Fact]
        public async Task Delete_deactivates_the_attachment_it_leaves_the_default_list_and_cannot_be_downloaded()
        {
            var admin = await AdminAsync();
            var id = await NewEquipmentAsync();
            var item = await UploadOkAsync(id, "a.pdf", PdfBytes());
            var attachmentId = item.GetProperty("id").GetInt64();

            var delete = await SendAsync(HttpMethod.Delete, $"/api/Attachments/{attachmentId}", bearer: admin);
            Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
            var json = await ReadJsonAsync(delete);
            Assert.Equal(JsonValueKindNull, json.GetProperty("data").ValueKind);
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("message").GetString()));

            Assert.Equal(0, DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}", bearer: admin))).GetArrayLength());
            var inactive = DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}?isActive=false", bearer: admin)));
            Assert.Equal(attachmentId, inactive[0].GetProperty("id").GetInt64());
            Assert.False(inactive[0].GetProperty("isActive").GetBoolean());

            await AssertErrorAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/{attachmentId}/download", bearer: admin), HttpStatusCode.NotFound, "not_found");
            // the row and its file stay (soft delete)
            Assert.True(InMemoryAttachmentBlobStore.Files.ContainsKey(await StreamIdOfAsync(attachmentId)));
        }

        private static readonly System.Text.Json.JsonValueKind JsonValueKindNull = System.Text.Json.JsonValueKind.Null;

        // ------------------------------------------------------------------------------- permissions

        [Fact]
        public async Task The_entitys_own_permissions_guard_each_endpoint_and_anonymous_callers_get_401()
        {
            var admin = await AdminAsync();
            var id = await NewEquipmentAsync();
            var item = await UploadOkAsync(id, "a.pdf", PdfBytes());
            var attachmentId = item.GetProperty("id").GetInt64();

            async Task<string> TokenWith(string suffix, params string[] permissions)
            {
                var userName = "att_" + suffix + Guid.NewGuid().ToString("N")[..4];
                var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
                var roleId = await CreateRoleAsync(admin, "AttRole_" + userName, permissions);
                await AssignRoleAsync(admin, userId, roleId);
                return (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            }

            var viewer = await TokenWith("v", "Equipments.View");
            var editor = await TokenWith("e", "Equipments.View", "Equipments.Update");
            var nobody = await TokenWith("n", "Brands.View");

            // View: list and download only
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}", bearer: viewer)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/Attachments/{attachmentId}/download", bearer: viewer)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await UploadAsync(viewer, id, "b.pdf", PdfBytes())).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, $"/api/Attachments/{attachmentId}", bearer: viewer)).StatusCode);

            // Update: upload and deactivate (View is needed to look)
            Assert.Equal(HttpStatusCode.Created, (await UploadAsync(editor, id, "b.pdf", PdfBytes())).StatusCode);

            // an unrelated permission gives nothing
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}", bearer: nobody)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"/api/Attachments/{attachmentId}/download", bearer: nobody)).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Attachments/{attachmentId}", bearer: editor)).StatusCode);

            // anonymous
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/Attachments/{attachmentId}/download")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Delete, $"/api/Attachments/{attachmentId}")).StatusCode);
        }
    }
}
