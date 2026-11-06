using ClosedXML.Excel;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Every answer that shows an equipment shows the same facts: the technician together with where he works, the owner with its title,
    /// the status code, the branch's place in the hierarchy and the department that serves it; the lookups of the form are open.
    /// </summary>
    public class EquipmentConsistencyTests : ApiTestBase
    {
        public EquipmentConsistencyTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private sealed record Place(long ProvinceId, string ProvinceTitle, long CityId, string CityTitle, long DepartmentId, string DepartmentTitle);
        private async Task<Place> NewPlaceAsync()
        {
            var provinceTitle = Uniq("cnprov"); var cityTitle = Uniq("cncity"); var departmentTitle = Uniq("cndept");
            var province = await CatalogFixtures.PostAsync(this, "/api/Province", new { title = provinceTitle }, "id");
            var city = await CatalogFixtures.PostAsync(this, "/api/City", new { provinceId = province, title = cityTitle }, "id");
            var department = await CatalogFixtures.PostAsync(this, "/api/SupportingDepartment", new { cityId = city, title = departmentTitle }, "id");
            return new Place(province, provinceTitle, city, cityTitle, department, departmentTitle);
        }

        private async Task<long> NewTechnicianAsync(long? departmentId)
        {
            var userName = "cn" + Guid.NewGuid().ToString("N")[..10];
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Users", new
            {
                userName, password = "Passw0rd!x", firstName = "علی", lastName = "تکنسین", personnelCode = "PC-" + userName,
                postId = (long)PostCode.Technician, supportingDepartmentId = departmentId,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private sealed record Org(long Id, string Title, string ZoneTitle, string ManagementTitle, string FieldTitle);
        private async Task<Org> NewOrganizationAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var zone = new Zone { Title = Uniq("cnzone") };
            var management = new OrganizationManagement { Title = Uniq("cnmgmt"), Zone = zone };
            var field = new Field { Title = Uniq("cnfield"), OrganizationManagement = management };
            var organization = new Organization { Title = Uniq("cnorg"), Field = field, OrganizationType = new OrganizationType { Title = Uniq("cnotype"), CreatedByUserId = 1 } };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
            return new Org(organization.OrganizationId, organization.Title, zone.Title, management.Title, field.Title);
        }

        private async Task<long> NewEquipmentAsync(object extra)
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("cncat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("cntype"), code = "T" }, "id");
            var location = await CatalogFixtures.PostAsync(this, "/api/EquipmentLocationType", new { title = Uniq("cnloc"), code = "L" }, "id");
            var body = new Dictionary<string, object?>
            {
                ["yaasCode"] = Uniq("CNY"), ["owner"] = (int)EquipmentOwner.Bank, ["equipmentTypeId"] = type,
                ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock, ["currentEquipmentLocationTypeId"] = location,
            };
            foreach (var property in extra.GetType().GetProperties())
                body[property.Name] = property.GetValue(extra);
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
            Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<JsonElement> ListRowAsync(long id)
        {
            var list = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment?pageSize=100")));
            return list.EnumerateArray().Single(e => e.GetProperty("id").GetInt64() == id);
        }

        private static void AssertTechnicianLocation(JsonElement row, long technician, Place place, string prefix = "technician")
        {
            string Name(string suffix) => prefix + suffix;
            Assert.Equal(technician, row.GetProperty(prefix == "technician" ? "technicianUserId" : "technicianUserId").GetInt64());
            Assert.Equal(place.DepartmentId, row.GetProperty(Name("SupportingDepartmentId")).GetInt64());
            Assert.Equal(place.DepartmentTitle, row.GetProperty(Name("SupportingDepartmentTitle")).GetString());
            Assert.Equal(place.CityId, row.GetProperty(Name("CityId")).GetInt64());
            Assert.Equal(place.CityTitle, row.GetProperty(Name("CityTitle")).GetString());
            Assert.Equal(place.ProvinceId, row.GetProperty(Name("ProvinceId")).GetInt64());
            Assert.Equal(place.ProvinceTitle, row.GetProperty(Name("ProvinceTitle")).GetString());
        }

        // ------------------------------------------------------------------------------------ the technician's place

        [Fact]
        public async Task Every_answer_with_a_technician_also_says_where_he_works()
        {
            var place = await NewPlaceAsync();
            var technician = await NewTechnicianAsync(place.DepartmentId);
            var id = await NewEquipmentAsync(new { technicianUserId = technician });

            // list and detail
            var row = await ListRowAsync(id);
            AssertTechnicianLocation(row, technician, place);
            Assert.True(row.GetProperty("technicianIsActive").GetBoolean());
            Assert.Equal("تکنسین", row.GetProperty("technicianPostTitle").GetString());
            AssertTechnicianLocation(await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}")), technician, place);

            // the update and the move answer with the same fields
            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
            var move = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentEquipmentLocationTypeId = detail.GetProperty("currentEquipmentLocationTypeId").GetInt64(), technicianUserId = technician, description = "x", currentSupportingDepartmentId = place.DepartmentId });
            Assert.Equal(HttpStatusCode.OK, move.StatusCode);
            AssertTechnicianLocation(await ReadDataAsync(move), technician, place);

            // /{id}/technician has the city and province too
            var single = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician"));
            Assert.Equal(place.DepartmentTitle, single.GetProperty("supportingDepartmentTitle").GetString());
            Assert.Equal(place.CityTitle, single.GetProperty("cityTitle").GetString());
            Assert.Equal(place.ProvinceTitle, single.GetProperty("provinceTitle").GetString());

            // the dropdown
            var dropdown = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/technicians?pageSize=100&search=PC-cn")))
                .EnumerateArray().Single(t => t.GetProperty("id").GetInt64() == technician);
            Assert.Equal(place.DepartmentTitle, dropdown.GetProperty("supportingDepartmentTitle").GetString());
            Assert.Equal(place.CityTitle, dropdown.GetProperty("cityTitle").GetString());
            Assert.Equal(place.ProvinceTitle, dropdown.GetProperty("provinceTitle").GetString());

            // the history entry: the department it records, with its city and province, and where the technician works now
            var history = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history"))).EnumerateArray().First();
            Assert.Equal(place.CityTitle, history.GetProperty("currentCityTitle").GetString());
            Assert.Equal(place.ProvinceTitle, history.GetProperty("currentProvinceTitle").GetString());
            Assert.Equal(place.DepartmentTitle, history.GetProperty("technicianSupportingDepartmentTitle").GetString());
            Assert.Equal(place.CityTitle, history.GetProperty("technicianCityTitle").GetString());
            Assert.Equal(place.ProvinceTitle, history.GetProperty("technicianProvinceTitle").GetString());
        }

        [Fact]
        public async Task A_technician_without_a_department_and_an_equipment_without_a_technician_show_nulls_and_a_deactivated_technician_is_flagged()
        {
            var noDepartment = await NewTechnicianAsync(null);
            var id = await NewEquipmentAsync(new { technicianUserId = noDepartment });
            var row = await ListRowAsync(id);
            Assert.Equal(JsonValueKind.Null, row.GetProperty("technicianSupportingDepartmentTitle").ValueKind);
            Assert.Equal(JsonValueKind.Null, row.GetProperty("technicianCityTitle").ValueKind);

            var without = await ListRowAsync(await NewEquipmentAsync(new { }));
            Assert.Equal(JsonValueKind.Null, without.GetProperty("technicianIsActive").ValueKind);
            Assert.Equal(JsonValueKind.Null, without.GetProperty("technicianUserId").ValueKind);

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Users/{noDepartment}")).StatusCode);
            Assert.False((await ListRowAsync(id)).GetProperty("technicianIsActive").GetBoolean());
        }

        [Fact]
        public async Task The_excel_file_has_the_technicians_place_and_the_branchs_hierarchy_columns()
        {
            var place = await NewPlaceAsync();
            var technician = await NewTechnicianAsync(place.DepartmentId);
            var org = await NewOrganizationAsync();
            var tag = Guid.NewGuid().ToString("N")[..8];
            var id = await NewEquipmentAsync(new { technicianUserId = technician, currentOrganizationId = org.Id, yaasCode = $"XC-{tag}" });

            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/export?search=XC-{tag}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var workbook = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            var sheet = workbook.Worksheets.First();
            var headers = Enumerable.Range(1, sheet.LastColumnUsed()!.ColumnNumber()).Select(i => sheet.Cell(1, i).GetString()).ToList();
            string Cell(string header) => sheet.Cell(2, headers.IndexOf(header) + 1).GetString();

            Assert.Equal(place.DepartmentTitle, Cell("دپارتمان تکنسین"));
            Assert.Equal(place.CityTitle, Cell("شهر تکنسین"));
            Assert.Equal(place.ProvinceTitle, Cell("استان تکنسین"));
            Assert.Equal(org.ZoneTitle, Cell("ناحیه"));
            Assert.Equal(org.ManagementTitle, Cell("مدیریت شعب"));
            Assert.Equal(org.FieldTitle, Cell("حوزه"));
            Assert.Equal("بانک", Cell("مالک"));
            Assert.NotEqual(0, id);
        }

        // ------------------------------------------------------------------------------------ the other facts

        [Fact]
        public async Task The_owner_has_a_title_and_the_status_a_code_and_the_branch_shows_its_place_and_its_department()
        {
            var org = await NewOrganizationAsync();
            var place = await NewPlaceAsync();
            var scope = await SendAdminAsync(HttpMethod.Post, $"/api/SupportingDepartment/{place.DepartmentId}/organizations", new { organizationIds = new[] { org.Id } });
            Assert.Equal(HttpStatusCode.OK, scope.StatusCode);

            var id = await NewEquipmentAsync(new { currentOrganizationId = org.Id });
            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
            var row = await ListRowAsync(id);

            foreach (var json in new[] { detail, row })
            {
                Assert.Equal((int)EquipmentOwner.Bank, json.GetProperty("owner").GetInt32());
                Assert.Equal("بانک", json.GetProperty("ownerTitle").GetString());
                Assert.Equal("InStock", json.GetProperty("equipmentStatusCode").GetString());
                Assert.Equal(org.ZoneTitle, json.GetProperty("zoneTitle").GetString());
                Assert.Equal(org.ManagementTitle, json.GetProperty("organizationManagementTitle").GetString());
                Assert.Equal(org.FieldTitle, json.GetProperty("fieldTitle").GetString());
                Assert.True(json.GetProperty("zoneId").GetInt64() > 0);
                Assert.Equal(place.DepartmentId, json.GetProperty("supportingDepartmentId").GetInt64());
                Assert.Equal(place.DepartmentTitle, json.GetProperty("supportingDepartmentTitle").GetString());
            }

            // the owners lookup uses the same titles
            var owners = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/owners"))).EnumerateArray().ToDictionary(o => o.GetProperty("id").GetInt32(), o => o.GetProperty("title").GetString());
            var ownerRow = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/owners"))).EnumerateArray().First();
            Assert.Equal("Company", ownerRow.GetProperty("code").GetString());                       // the shape of every lookup item: id, title, code, isActive
            Assert.True(ownerRow.GetProperty("isActive").GetBoolean());
            Assert.False(ownerRow.TryGetProperty("name", out _));
            Assert.Equal("شرکت", owners[(int)EquipmentOwner.Company]);
            Assert.Equal("بانک", owners[(int)EquipmentOwner.Bank]);
        }

        [Fact]
        public async Task The_list_filters_by_the_department_that_serves_the_branch_separately_from_the_one_the_equipment_is_at()
        {
            var org = await NewOrganizationAsync();
            var serving = await NewPlaceAsync();
            var at = await NewPlaceAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/SupportingDepartment/{serving.DepartmentId}/organizations", new { organizationIds = new[] { org.Id } })).StatusCode);

            var served = await NewEquipmentAsync(new { currentOrganizationId = org.Id });
            var elsewhere = await NewEquipmentAsync(new { currentSupportingDepartmentId = at.DepartmentId });

            async Task<List<long>> Ids(string query) =>
                DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment?pageSize=100&{query}"))).EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).ToList();

            Assert.Equal(new[] { served }, await Ids($"supportingDepartmentId={serving.DepartmentId}"));
            Assert.Equal(new[] { elsewhere }, await Ids($"currentSupportingDepartmentId={at.DepartmentId}"));        // the equipment's own department
            Assert.Empty(await Ids($"supportingDepartmentId={at.DepartmentId}"));
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Get, "/api/Equipment?currentSupportingDepartmentId=0")).StatusCode);
        }

        // ------------------------------------------------------------------------------------ the form's lookups

        [Fact]
        public async Task Someone_who_may_only_create_equipment_can_still_read_the_lookups_of_the_form()
        {
            var admin = await AdminAsync();
            var role = await CreateRoleAsync(admin, "CnCreate" + Guid.NewGuid().ToString("N")[..4], "Equipments.Create");
            var userName = "cncreate" + Guid.NewGuid().ToString("N")[..6];
            var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
            await AssignRoleAsync(admin, userId, role);
            var token = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/Equipment/owners", bearer: token)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/Equipment/technicians", bearer: token)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/Equipment", bearer: token)).StatusCode);   // the list itself still needs View
        }
    }
}
