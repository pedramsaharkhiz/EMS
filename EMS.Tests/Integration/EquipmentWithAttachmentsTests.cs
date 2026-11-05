using EMS.Application.Common.Exceptions;
using EMS.Application.Services.Attachment;
using EMS.Domain.Enums;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// POST /api/Equipment/with-attachments: the equipment JSON first, then up to 5 files, all in one call. (These tests run on the in-memory
    /// database, which ignores transactions, so they prove the rules and the shape of the answers; that a refused file leaves no equipment
    /// behind is proven by the manual SQL Server test in SqlEquipmentRegistrationTests.)
    /// </summary>
    public class EquipmentWithAttachmentsTests : ApiTestBase
    {
        public EquipmentWithAttachmentsTests(EmsApiFactory factory) : base(factory) { }

        private const string Url = "/api/Equipment/with-attachments";

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private static readonly byte[] PngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(Enumerable.Range(0, 2000).Select(i => (byte)(i % 251))).ToArray();
        private static byte[] PdfBytes(int size = 3000) => Encoding.ASCII.GetBytes("%PDF-1.7\n").Concat(Enumerable.Range(0, size).Select(i => (byte)(i % 200 + 32))).ToArray();

        private sealed record Fixture(long TypeId, long LocationTypeId);
        private async Task<Fixture> NewFixtureAsync()
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("wacat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("watype"), code = "T" }, "id");
            var location = await CatalogFixtures.PostAsync(this, "/api/EquipmentLocationType", new { title = Uniq("waloc"), code = "L" }, "id");
            return new Fixture(type, location);
        }

        private static Dictionary<string, object?> Body(Fixture f, string? yaas = null) => new()
        {
            ["yaasCode"] = yaas ?? Uniq("WAY"),
            ["owner"] = (int)EquipmentOwner.Company,
            ["equipmentTypeId"] = f.TypeId,
            ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock,
            ["currentEquipmentLocationTypeId"] = f.LocationTypeId,
        };

        private sealed record Part(string Name, string? FileName, byte[] Bytes);

        private static Part Json(object body) => new("equipment", null, JsonSerializer.SerializeToUtf8Bytes(body));
        private static Part Text(string name, string text) => new(name, null, Encoding.UTF8.GetBytes(text));
        private static Part File(string fileName, byte[] bytes) => new("file", fileName, bytes);

        private async Task<HttpResponseMessage> PostAsync(string? bearer, params Part[] parts)
        {
            using var form = new MultipartFormDataContent();
            foreach (var part in parts)
            {
                var content = new ByteArrayContent(part.Bytes);
                if (part.FileName is null) form.Add(content, part.Name);
                else
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");   // what the client claims is irrelevant
                    form.Add(content, part.Name, part.FileName);
                }
            }
            var request = new HttpRequestMessage(HttpMethod.Post, Url) { Content = form };
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> PostAdminAsync(params Part[] parts) => await PostAsync(await AdminAsync(), parts);

        private async Task<int> CountByYaasAsync(string yaas) =>
            DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Equipment?yaasCode={yaas}", bearer: await AdminAsync()))).GetArrayLength();

        // ------------------------------------------------------------------------------------------ success

        [Fact]
        public async Task An_equipment_is_registered_with_its_files_in_one_call()
        {
            var f = await NewFixtureAsync();
            var pdf = PdfBytes();
            var response = await PostAdminAsync(Json(Body(f)), Text("description", "نمای کلی"), File("a.pdf", pdf), File("b.png", PngBytes));

            Assert.True(response.StatusCode == HttpStatusCode.Created, $"-> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            Assert.NotNull(response.Headers.Location);
            var json = await ReadDataAsync(response);

            // everything a plain registration answers, plus the attachments
            Assert.True(json.GetProperty("id").GetInt64() > 0);
            Assert.Equal((long)EquipmentStatusCode.InStock, json.GetProperty("equipmentStatusId").GetInt64());
            Assert.False(string.IsNullOrEmpty(json.GetProperty("rowVersion").GetString()));
            var attachments = json.GetProperty("attachments").EnumerateArray().ToList();
            Assert.Equal(2, attachments.Count);
            Assert.Equal("a.pdf", attachments[0].GetProperty("fileName").GetString());
            Assert.Equal("نمای کلی", attachments[0].GetProperty("description").GetString());       // the text part before the file
            Assert.Equal(JsonValueKind.Null, attachments[1].GetProperty("description").ValueKind);   // and only for that file
            Assert.Equal(pdf.Length, attachments[0].GetProperty("sizeBytes").GetInt64());
            Assert.Equal("Equipments", attachments[0].GetProperty("tableName").GetString());

            // they are ordinary attachments of that equipment
            var id = json.GetProperty("id").GetInt64();
            var list = DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Attachments/Equipments/{id}", bearer: await AdminAsync())));
            Assert.Equal(2, list.GetArrayLength());
            var download = await SendAsync(HttpMethod.Get, $"/api/Attachments/{attachments[0].GetProperty("id").GetInt64()}/download", bearer: await AdminAsync());
            Assert.Equal(pdf, await download.Content.ReadAsByteArrayAsync());

            // and the equipment has its first location history entry like any other
            var history = DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history", bearer: await AdminAsync())));
            Assert.Equal(1, history.GetArrayLength());
        }

        [Fact]
        public async Task Files_are_optional_and_the_plain_endpoint_is_untouched()
        {
            var f = await NewFixtureAsync();
            var without = await PostAdminAsync(Json(Body(f)));
            Assert.Equal(HttpStatusCode.Created, without.StatusCode);
            Assert.Equal(0, (await ReadDataAsync(without)).GetProperty("attachments").GetArrayLength());

            var plain = await SendAsync(HttpMethod.Post, "/api/Equipment", Body(f), bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.Created, plain.StatusCode);
            Assert.False((await ReadDataAsync(plain)).TryGetProperty("attachments", out _));      // the plain answer has no such field
        }

        [Fact]
        public async Task Up_to_five_files_are_accepted_and_the_sixth_is_refused()
        {
            var f = await NewFixtureAsync();
            var five = await PostAdminAsync(new[] { Json(Body(f)) }.Concat(Enumerable.Range(0, AttachmentRules.MaxFilesPerRegistration).Select(i => File($"f{i}.pdf", PdfBytes()))).ToArray());
            Assert.Equal(HttpStatusCode.Created, five.StatusCode);
            Assert.Equal(5, (await ReadDataAsync(five)).GetProperty("attachments").GetArrayLength());

            var six = await PostAdminAsync(new[] { Json(Body(f)) }.Concat(Enumerable.Range(0, AttachmentRules.MaxFilesPerRegistration + 1).Select(i => File($"f{i}.pdf", PdfBytes()))).ToArray());
            Assert.Equal(HttpStatusCode.BadRequest, six.StatusCode);
            Assert.Equal(ErrorCodes.AttachmentsPerRequestExceeded, ErrorCodeOf(await ReadJsonAsync(six)));
        }

        // ------------------------------------------------------------------------------------------ refusals

        [Fact]
        public async Task A_refused_file_gives_the_attachment_error_codes()
        {
            var f = await NewFixtureAsync();
            var wrongType = await PostAdminAsync(Json(Body(f)), File("virus.exe", PdfBytes()));
            Assert.Equal(HttpStatusCode.BadRequest, wrongType.StatusCode);
            Assert.Equal("attachment_type_not_allowed", ErrorCodeOf(await ReadJsonAsync(wrongType)));

            var mismatch = await PostAdminAsync(Json(Body(f)), File("fake.pdf", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
            Assert.Equal("attachment_content_mismatch", ErrorCodeOf(await ReadJsonAsync(mismatch)));

            var tooBig = await PostAdminAsync(Json(Body(f)), File("big.pdf", PdfBytes((int)AttachmentRules.MaxFileSizeBytes + 10)));
            Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);
            Assert.Equal("attachment_too_large", ErrorCodeOf(await ReadJsonAsync(tooBig)));

            var longDescription = await PostAdminAsync(Json(Body(f)), Text("description", new string('d', AttachmentRules.MaxDescriptionLength + 1)), File("a.pdf", PdfBytes()));
            Assert.Equal(HttpStatusCode.BadRequest, longDescription.StatusCode);
            Assert.Contains("description", ErrorFieldsOf(await ReadJsonAsync(longDescription)));
        }

        [Fact]
        public async Task A_bad_equipment_is_refused_before_any_file_is_read_with_the_usual_validation_errors()
        {
            var f = await NewFixtureAsync();

            var invalid = Body(f, yaas: ""); invalid["equipmentStatusId"] = 0;
            var response = await PostAdminAsync(Json(invalid), File("a.pdf", PdfBytes()));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("validation_failed", ErrorCodeOf(json));
            Assert.Contains("yaasCode", ErrorFieldsOf(json));
            Assert.Contains("equipmentStatusId", ErrorFieldsOf(json));

            // a business error (unknown status) is the same as for the plain endpoint, and no file is stored
            var unknown = Body(f); unknown["equipmentStatusId"] = 99999999;
            var notFound = await PostAdminAsync(Json(unknown), File("a.pdf", PdfBytes()));
            Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(notFound)));

            // a taken Yaas code
            var yaas = Uniq("TAKEN");
            Assert.Equal(HttpStatusCode.Created, (await PostAdminAsync(Json(Body(f, yaas)))).StatusCode);
            var duplicate = await PostAdminAsync(Json(Body(f, yaas)), File("a.pdf", PdfBytes()));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("duplicate_yaas_code", ErrorCodeOf(await ReadJsonAsync(duplicate)));
            Assert.Equal(1, await CountByYaasAsync(yaas));
        }

        [Fact]
        public async Task The_equipment_part_must_come_first_and_be_json()
        {
            var f = await NewFixtureAsync();

            var fileFirst = await PostAdminAsync(File("a.pdf", PdfBytes()), Json(Body(f)));
            Assert.Equal(HttpStatusCode.BadRequest, fileFirst.StatusCode);
            var fileFirstJson = await ReadJsonAsync(fileFirst);
            Assert.Equal("validation_failed", ErrorCodeOf(fileFirstJson));
            Assert.Contains("equipment", ErrorFieldsOf(fileFirstJson));

            var notJson = await PostAdminAsync(Text("equipment", "this is not json"));
            Assert.Equal(HttpStatusCode.BadRequest, notJson.StatusCode);
            Assert.Contains("equipment", ErrorFieldsOf(await ReadJsonAsync(notJson)));

            var empty = await PostAdminAsync(Text("equipment", "null"));
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);

            var noParts = await SendAsync(HttpMethod.Post, Url, new { }, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, noParts.StatusCode);                 // not multipart
        }

        // ------------------------------------------------------------------------------------------ permissions

        [Fact]
        public async Task Registering_with_files_needs_only_the_create_permission()
        {
            var admin = await AdminAsync();
            var f = await NewFixtureAsync();

            var plainName = "waplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, plainName, "Passw0rd!x");
            var plain = (await LoginAsync(plainName, "Passw0rd!x")).AccessToken;
            Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(plain, Json(Body(f)), File("a.pdf", PdfBytes()))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await PostAsync(null, Json(Body(f)))).StatusCode);

            // Equipments.Create alone is enough, although uploading to an existing equipment needs Equipments.Update
            var role = await CreateRoleAsync(admin, "WaCreate" + Guid.NewGuid().ToString("N")[..4], "Equipments.Create");
            var userName = "wareg" + Guid.NewGuid().ToString("N")[..6];
            var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
            await AssignRoleAsync(admin, userId, role);
            var token = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            var created = await PostAsync(token, Json(Body(f)), File("a.pdf", PdfBytes()));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await ReadDataAsync(created)).GetProperty("id").GetInt64();

            using var form = new MultipartFormDataContent { { new ByteArrayContent(PdfBytes()), "file", "later.pdf" } };
            var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Attachments/Equipments/{id}") { Content = form };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.SendAsync(request)).StatusCode);   // the separate upload still needs Update
        }
    }
}
