using EMS.Application.Common.Exceptions;
using EMS.Domain;
using EMS.Domain.Enums;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>POST /api/Equipment/bulk: the same rules as a single create, per item, all or nothing.</summary>
    public class EquipmentBulkCreateTests : ApiTestBase
    {
        public EquipmentBulkCreateTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private static long InStock => (long)EquipmentStatusCode.InStock;

        private async Task<long> PostIdAsync(string url, object body) => await CatalogFixtures.PostAsync(this, url, body, "id");

        private async Task<long> NewTypeAsync()
        {
            var category = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("bkcat") });
            return await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("bktype"), code = "T" });
        }

        private async Task<long> NewDepartmentAsync() => await PostIdAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("bkloc") });

        private sealed record Fixture(long TypeId, long DepartmentId);
        private async Task<Fixture> NewFixtureAsync() => new(await NewTypeAsync(), await NewDepartmentAsync());

        private static Dictionary<string, object?> Item(Fixture f, string? yaas = null, long? status = null) => new()
        {
            ["yaasCode"] = yaas ?? Uniq("BY"),
            ["owner"] = 1,
            ["equipmentTypeId"] = f.TypeId,
            ["equipmentStatusId"] = status ?? InStock,
            ["currentSupportingDepartmentId"] = f.DepartmentId,
        };

        private async Task<HttpResponseMessage> BulkAsync(params object[] items) =>
            await SendAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items }, bearer: await AdminAsync());

        private static List<(string Field, string Message)> Details(JsonElement envelope) =>
            envelope.GetProperty("error").GetProperty("details").EnumerateArray()
                .Select(d => (d.GetProperty("field").GetString()!, d.GetProperty("message").GetString()!)).ToList();

        private async Task<int> CountByYaasAsync(string yaas)
        {
            var list = await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Equipment?yaasCode={yaas}", bearer: await AdminAsync()));
            return DataOf(list).GetArrayLength();
        }

        [Fact]
        public async Task Creates_every_item_in_order_with_its_first_location_history()
        {
            var f = await NewFixtureAsync();
            var codes = Enumerable.Range(0, 3).Select(_ => Uniq("BO")).ToList();
            var response = await BulkAsync(codes.Select(c => (object)Item(f, c)).ToArray());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            var data = await ReadDataAsync(response);
            Assert.Equal(codes, data.EnumerateArray().Select(e => e.GetProperty("yaasCode").GetString()!).ToList());
            Assert.All(data.EnumerateArray(), e =>
            {
                Assert.True(e.GetProperty("id").GetInt64() > 0);
                Assert.False(string.IsNullOrEmpty(e.GetProperty("rowVersion").GetString()));
                Assert.Equal(InStock, e.GetProperty("equipmentStatusId").GetInt64());
            });

            var id = data[0].GetProperty("id").GetInt64();
            var history = DataOf(await ReadJsonAsync(await SendAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history", bearer: await AdminAsync())));
            Assert.Equal(1, history.GetArrayLength());
            Assert.Equal("ثبت اولیه تجهیز", history[0].GetProperty("description").GetString());
        }

        [Fact]
        public async Task A_single_item_works_and_empty_or_oversized_lists_are_refused()
        {
            var f = await NewFixtureAsync();
            Assert.Equal(HttpStatusCode.Created, (await BulkAsync(Item(f))).StatusCode);

            var empty = await BulkAsync();
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
            Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(empty)));

            var tooMany = await BulkAsync(Enumerable.Range(0, EquipmentConstraints.BulkCreateMaxItems + 1).Select(_ => (object)Item(f)).ToArray());
            Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);
            Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(tooMany)));

            var noBody = await SendAsync(HttpMethod.Post, "/api/Equipment/bulk", new { }, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.BadRequest, noBody.StatusCode);
        }

        [Fact]
        public async Task Field_errors_name_the_item_and_nothing_is_saved()
        {
            var f = await NewFixtureAsync();
            var good = Uniq("BG");
            var bad = Item(f, "");                       // missing yaas code
            var response = await BulkAsync(Item(f, good), bad);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("validation_failed", ErrorCodeOf(json));
            Assert.Contains("items[1].yaasCode", ErrorFieldsOf(json));
            Assert.DoesNotContain(ErrorFieldsOf(json), x => x.StartsWith("items[0]"));
            Assert.Equal(0, await CountByYaasAsync(good));
        }

        [Fact]
        public async Task The_same_yaas_code_twice_in_one_request_is_fine_but_a_repeated_serial_or_asset_number_is_refused_case_insensitively()
        {
            var f = await NewFixtureAsync();
            var yaas = Uniq("BD");
            var ok = await BulkAsync(Item(f, yaas), Item(f, yaas.ToLowerInvariant()));
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);                              // the Yaas code is not unique

            var serial = Uniq("SER");
            var asset = Uniq("AST");
            var first = Item(f); first["serialNumber"] = serial; first["assetNumber"] = asset;
            var other = Item(f); other["serialNumber"] = Uniq("SER2");
            var repeated = Item(f); repeated["serialNumber"] = "  " + serial.ToLowerInvariant(); repeated["assetNumber"] = asset.ToUpperInvariant();

            var response = await BulkAsync(first, other, repeated);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var details = Details(await ReadJsonAsync(response));
            Assert.Contains(details, d => d.Field == "items[2].serialNumber");
            Assert.Contains(details, d => d.Field == "items[2].assetNumber");
            Assert.DoesNotContain(details, d => d.Field.StartsWith("items[0]") || d.Field.StartsWith("items[1]"));

            // rows without a serial or asset number never repeat
            Assert.Equal(HttpStatusCode.Created, (await BulkAsync(Item(f), Item(f))).StatusCode);
        }

        [Fact]
        public async Task Business_errors_are_reported_for_every_bad_row_at_once_and_nothing_is_saved()
        {
            var f = await NewFixtureAsync();
            var admin = await AdminAsync();

            var taken = Uniq("BT");
            var holder = Item(f); holder["serialNumber"] = taken;
            Assert.Equal(HttpStatusCode.Created, (await BulkAsync(holder)).StatusCode);

            var inactiveStatus = await PostIdAsync("/api/EquipmentStatus", new { title = Uniq("bkstat") });
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/EquipmentStatus/{inactiveStatus}", bearer: admin)).StatusCode);

            var okCode = Uniq("BK");
            var wrongType = Item(f, Uniq("B1")); wrongType["equipmentTypeId"] = 99999999;
            var wrongOrg = Item(f, Uniq("B2")); wrongOrg.Remove("currentSupportingDepartmentId"); wrongOrg["currentOrganizationId"] = 99999999;
            var wrongTech = Item(f, Uniq("B3")); wrongTech["technicianUserId"] = 99999999;

            var clash = Item(f, Uniq("B9")); clash["serialNumber"] = taken.ToUpperInvariant();
            var response = await BulkAsync(
                Item(f, okCode),                         // 0: fine
                clash,                                   // 1: its serial number is held by an existing equipment
                Item(f, Uniq("B0"), inactiveStatus),     // 2: inactive status
                wrongType,                               // 3
                wrongOrg,                                // 4
                wrongTech);                              // 5

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal(ErrorCodes.BulkItemsInvalid, ErrorCodeOf(json));
            Assert.Contains("5 ردیف", json.GetProperty("message").GetString());

            var details = Details(json).ToDictionary(d => d.Field, d => d.Message);
            Assert.Contains("سریال نامبر", details["items[1].serialNumber"]);
            Assert.Contains("غیرفعال", details["items[2].equipmentStatusId"]);
            Assert.Contains("یافت نشد", details["items[3].equipmentTypeId"]);
            Assert.Contains("یافت نشد", details["items[4].currentOrganizationId"]);
            Assert.Contains("تکنسین", details["items[5].technicianUserId"]);
            Assert.DoesNotContain(details.Keys, k => k.StartsWith("items[0]"));
            Assert.Equal(0, await CountByYaasAsync(okCode));         // the good row was not saved either
        }

        [Fact]
        public async Task An_inactive_technician_is_refused_and_an_active_one_is_accepted()
        {
            var f = await NewFixtureAsync();
            var admin = await AdminAsync();
            var active = await CreateUserAsync(admin, "bt_" + Guid.NewGuid().ToString("N")[..10], "Passw0rd!x", postId: (long)PostCode.Technician);
            var inactive = await CreateUserAsync(admin, "bt_" + Guid.NewGuid().ToString("N")[..10], "Passw0rd!x");
            await SendAsync(HttpMethod.Delete, $"/api/Users/{inactive}", bearer: admin);

            var a = Item(f); a["technicianUserId"] = active;
            var b = Item(f); b["technicianUserId"] = inactive;

            var refused = await BulkAsync(a, b);
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Contains("غیرفعال", Details(await ReadJsonAsync(refused)).Single(d => d.Field == "items[1].technicianUserId").Message);

            var accepted = await BulkAsync(a);
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
            Assert.Equal(active, (await ReadDataAsync(accepted))[0].GetProperty("technicianUserId").GetInt64());
        }

        [Fact]
        public async Task Needs_the_create_permission_and_a_login()
        {
            var admin = await AdminAsync();
            var userName = "bkplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            var f = await NewFixtureAsync();

            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items = new[] { Item(f) } }, bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items = new[] { Item(f) } })).StatusCode);
        }
    }
}
