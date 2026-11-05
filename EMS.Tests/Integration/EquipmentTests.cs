using ClosedXML.Excel;
using EMS.Application.Common.Globalization;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>Equipment API: create / read / update / soft delete / restore, move with history, filters, technicians and the Excel export.</summary>
    public class EquipmentTests : ApiTestBase
    {
        public EquipmentTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private static long InStock => (long)EquipmentStatusCode.InStock;

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<long> PostIdAsync(string url, object body) =>
            await CatalogFixtures.PostAsync(this, url, body, "id");

        // ------------------------------------------------------------------------------------------ fixtures

        private sealed record Catalogs(long CategoryId, string CategoryTitle, long TypeId, string TypeTitle, long BrandId, string BrandTitle, long ModelId, string ModelTitle);

        private async Task<Catalogs> NewCatalogsAsync()
        {
            var categoryTitle = Uniq("eqcat"); var typeTitle = Uniq("eqtype"); var brandTitle = Uniq("eqbrand"); var modelTitle = Uniq("eqmodel");
            var category = await PostIdAsync("/api/EquipmentCategory", new { title = categoryTitle });
            var type = await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = category, title = typeTitle, code = "T" });
            var brand = await PostIdAsync("/api/Brand", new { title = brandTitle, code = "B" });
            var model = await PostIdAsync("/api/EquipmentModel", new { equipmentTypeId = type, brandId = brand, title = modelTitle, code = "M" });
            return new Catalogs(category, categoryTitle, type, typeTitle, brand, brandTitle, model, modelTitle);
        }

        private Task<long> NewLocationTypeAsync() => PostIdAsync("/api/EquipmentLocationType", new { title = Uniq("eqloc"), code = "L" });

        private async Task<(long ProvinceId, string ProvinceTitle, long CityId, string CityTitle, long DepartmentId, string DepartmentTitle)> NewDepartmentAsync()
        {
            var provinceTitle = Uniq("eqprov"); var cityTitle = Uniq("eqcity"); var departmentTitle = Uniq("eqdept");
            var province = await PostIdAsync("/api/Province", new { title = provinceTitle });
            var city = await PostIdAsync("/api/City", new { provinceId = province, title = cityTitle });
            var department = await PostIdAsync("/api/SupportingDepartment", new { cityId = city, title = departmentTitle });
            return (province, provinceTitle, city, cityTitle, department, departmentTitle);
        }

        private sealed record Org(long OrganizationId, string Title, long FieldId, long ManagementId, long ZoneId);

        /// <summary>Organizations come from outside the API: insert a Zone → Management → Field → Organization chain directly.</summary>
        private async Task<Org> NewOrganizationAsync(bool active = true)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var zone = new Zone { Title = Uniq("eqzone") };
            var management = new OrganizationManagement { Title = Uniq("eqmgmt"), Zone = zone };
            var field = new Field { Title = Uniq("eqfield"), OrganizationManagement = management };
            var type = new OrganizationType { Title = Uniq("eqotype"), CreatedByUserId = 1 };
            var organization = new Organization { Title = Uniq("eqorg"), Field = field, OrganizationType = type, IsActive = active };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
            return new Org(organization.OrganizationId, organization.Title, field.FieldId, management.OrganizationManagementId, zone.ZoneId);
        }

        private async Task<(long Id, string FullName)> NewTechnicianAsync()
        {
            var userName = "tc_" + Guid.NewGuid().ToString("N")[..10];
            var id = await CreateUserAsync(await AdminAsync(), userName, "Passw0rd!x", postId: (long)PostCode.Technician);   // only users with the post "Technician" can be technicians
            return (id, "Test User");
        }

        private async Task<JsonElement> CreateAsync(object body)
        {
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"create -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadDataAsync(response);
        }

        private async Task<Dictionary<string, object?>> BodyAsync(string? yaas = null, long? typeId = null, long? modelId = null, long? status = null, long? locationType = null)
        {
            if (typeId is null && modelId is null)
                typeId = (await NewCatalogsAsync()).TypeId;

            return new Dictionary<string, object?>
            {
                ["yaasCode"] = yaas ?? Uniq("Y"),
                ["owner"] = 1,
                ["equipmentTypeId"] = typeId,
                ["equipmentModelId"] = modelId,
                ["equipmentStatusId"] = status ?? InStock,
                ["currentEquipmentLocationTypeId"] = locationType ?? await NewLocationTypeAsync(),
            };
        }

        private async Task<JsonElement> ListAsync(string query)
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment?{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadJsonAsync(response);
        }

        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();
        private static List<long> Ids(JsonElement page) => DataOf(page).EnumerateArray().Select(Id).ToList();

        private async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, ErrorCodeOf(await ReadJsonAsync(response)));
        }

        // ------------------------------------------------------------------------------------------ create / read

        [Fact]
        public async Task Create_returns_201_with_names_a_location_header_and_a_first_history_entry()
        {
            var c = await NewCatalogsAsync();
            var dept = await NewDepartmentAsync();
            var org = await NewOrganizationAsync();
            var tech = await NewTechnicianAsync();
            var locationType = await NewLocationTypeAsync();
            var yaas = Uniq("Y");

            var body = await BodyAsync(yaas, modelId: c.ModelId, locationType: locationType);
            body["serialNumber"] = "  SN-000123 ";
            body["assetNumber"] = "AS-1";
            body["currentOrganizationId"] = org.OrganizationId;
            body["currentSupportingDepartmentId"] = dept.DepartmentId;
            body["technicianUserId"] = tech.Id;
            body["description"] = "توضیح";

            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var json = await ReadDataAsync(response);
            Assert.EndsWith($"/api/Equipment/{Id(json)}", response.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);

            Assert.Equal(yaas, json.GetProperty("yaasCode").GetString());
            Assert.Equal("SN-000123", json.GetProperty("serialNumber").GetString());                   // trimmed
            Assert.Equal(c.ModelId, json.GetProperty("equipmentModelId").GetInt64());
            Assert.Equal(JsonValueKind.Null, json.GetProperty("equipmentTypeId").ValueKind);
            Assert.Equal(c.ModelTitle, json.GetProperty("equipmentModelTitle").GetString());
            Assert.Equal(c.BrandTitle, json.GetProperty("brandTitle").GetString());
            Assert.Equal(c.TypeId, json.GetProperty("effectiveTypeId").GetInt64());                    // the type of its model
            Assert.Equal(c.TypeTitle, json.GetProperty("effectiveTypeTitle").GetString());
            Assert.Equal(c.CategoryTitle, json.GetProperty("categoryTitle").GetString());
            Assert.Equal("موجود در انبار", json.GetProperty("equipmentStatusTitle").GetString());
            Assert.Equal(org.Title, json.GetProperty("currentOrganizationTitle").GetString());
            Assert.Equal(dept.DepartmentTitle, json.GetProperty("currentSupportingDepartmentTitle").GetString());
            Assert.Equal(dept.CityTitle, json.GetProperty("cityTitle").GetString());
            Assert.Equal(dept.ProvinceTitle, json.GetProperty("provinceTitle").GetString());
            Assert.Equal("Test User", json.GetProperty("technicianName").GetString());
            Assert.True(json.GetProperty("isActive").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("rowVersion").GetString()));

            var history = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{Id(json)}/location-history"))).EnumerateArray().ToList();
            var first = Assert.Single(history);
            Assert.Equal(locationType, first.GetProperty("equipmentLocationTypeId").GetInt64());
            Assert.Equal(tech.Id, first.GetProperty("technicianUserId").GetInt64());
            Assert.Equal("ثبت اولیه تجهیز", first.GetProperty("description").GetString());
            Assert.Equal("System Admin", first.GetProperty("changedByName").GetString());                  // who created it (the seeded administrator)
        }

        [Fact]
        public async Task A_type_equipment_reports_its_own_type_and_category_and_has_no_model_or_brand()
        {
            var c = await NewCatalogsAsync();
            var json = await CreateAsync(await BodyAsync(typeId: c.TypeId));

            Assert.Equal(c.TypeId, json.GetProperty("equipmentTypeId").GetInt64());
            Assert.Equal(c.TypeId, json.GetProperty("effectiveTypeId").GetInt64());
            Assert.Equal(c.CategoryTitle, json.GetProperty("categoryTitle").GetString());
            Assert.Equal(JsonValueKind.Null, json.GetProperty("equipmentModelId").ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("brandTitle").ValueKind);
        }

        [Fact]
        public async Task Get_by_id_returns_the_same_detail_and_an_unknown_id_is_404()
        {
            var created = await CreateAsync(await BodyAsync());
            var get = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{Id(created)}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(created.GetProperty("yaasCode").GetString(), (await ReadDataAsync(get)).GetProperty("yaasCode").GetString());

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/987654321"), HttpStatusCode.NotFound, "not_found");
        }

        // ------------------------------------------------------------------------------------------ validation and references

        [Fact]
        public async Task Exactly_one_of_type_and_model_is_required()
        {
            var c = await NewCatalogsAsync();

            var both = await BodyAsync(typeId: c.TypeId, modelId: c.ModelId);
            var neither = await BodyAsync();
            neither["equipmentTypeId"] = null;

            foreach (var body in new[] { both, neither })
            {
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(response)));
            }
        }

        [Fact]
        public async Task Field_rules_are_validated_in_persian()
        {
            var body = await BodyAsync();
            body["yaasCode"] = "";
            body["serialNumber"] = new string('x', 51);
            body["description"] = new string('d', 501);
            body["equipmentStatusId"] = 0;
            body["currentEquipmentLocationTypeId"] = 0;

            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var fields = ErrorFieldsOf(await ReadJsonAsync(response));
            foreach (var field in new[] { "yaasCode", "serialNumber", "description", "equipmentStatusId", "currentEquipmentLocationTypeId" })
                Assert.Contains(field, fields);
        }

        [Fact]
        public async Task Unknown_references_are_404_and_deactivated_ones_are_400()
        {
            var unknownStatus = await BodyAsync(status: 987654321);
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", unknownStatus), HttpStatusCode.NotFound, "parent_not_found");

            var unknownModel = await BodyAsync(modelId: 987654321);
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", unknownModel), HttpStatusCode.NotFound, "parent_not_found");

            var inactiveOrg = await NewOrganizationAsync(active: false);
            var withOrg = await BodyAsync();
            withOrg["currentOrganizationId"] = inactiveOrg.OrganizationId;
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", withOrg), HttpStatusCode.BadRequest, "parent_inactive");

            var withUnknownTechnician = await BodyAsync();
            withUnknownTechnician["technicianUserId"] = 987654321;
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", withUnknownTechnician), HttpStatusCode.NotFound, "parent_not_found");

            var deactivatedLocationType = await NewLocationTypeAsync();
            await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentLocationType/{deactivatedLocationType}");
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", await BodyAsync(locationType: deactivatedLocationType)), HttpStatusCode.BadRequest, "parent_inactive");
        }

        [Fact]
        public async Task An_inactive_technician_cannot_be_chosen()
        {
            var tech = await NewTechnicianAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                (await db.Users.SingleAsync(u => u.Id == tech.Id)).IsActive = false;
                await db.SaveChangesAsync();
            }

            var body = await BodyAsync();
            body["technicianUserId"] = tech.Id;
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body), HttpStatusCode.BadRequest, "parent_inactive");
        }

        // ------------------------------------------------------------------------------------------ Yaas code uniqueness

        [Fact]
        public async Task The_yaas_code_is_unique_among_active_equipment_ignoring_case_and_spaces()
        {
            var yaas = Uniq("Y");
            await CreateAsync(await BodyAsync(yaas));

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", await BodyAsync(yaas)), HttpStatusCode.Conflict, "duplicate_yaas_code");
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", await BodyAsync("  " + yaas.ToLowerInvariant() + " ")), HttpStatusCode.Conflict, "duplicate_yaas_code");
        }

        [Fact]
        public async Task A_deactivated_equipment_frees_its_yaas_code_and_cannot_be_restored_onto_a_taken_one()
        {
            var yaas = Uniq("Y");
            var first = await CreateAsync(await BodyAsync(yaas));
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{Id(first)}")).StatusCode);

            var second = await CreateAsync(await BodyAsync(yaas));                         // the code is free again
            Assert.NotEqual(Id(first), Id(second));

            var restore = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(first)}", UpdateBody(first));
            await AssertErrorAsync(restore, HttpStatusCode.Conflict, "duplicate_yaas_code");
        }

        [Fact]
        public async Task Changing_the_yaas_code_to_another_active_ones_code_is_a_conflict_but_keeping_your_own_is_fine()
        {
            var a = await CreateAsync(await BodyAsync(Uniq("Y")));
            var b = await CreateAsync(await BodyAsync(Uniq("Y")));

            var own = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(a)}", UpdateBody(a, description: "x"));
            Assert.Equal(HttpStatusCode.OK, own.StatusCode);

            var clash = UpdateBody(a);
            clash["yaasCode"] = b.GetProperty("yaasCode").GetString()!.ToUpperInvariant();
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(a)}", clash), HttpStatusCode.Conflict, "duplicate_yaas_code");
        }

        // ------------------------------------------------------------------------------------------ update

        private static Dictionary<string, object?> UpdateBody(JsonElement current, string? description = null, bool? isActive = null, string? rowVersion = null)
        {
            long? Number(string name) => current.GetProperty(name).ValueKind == JsonValueKind.Null ? null : current.GetProperty(name).GetInt64();
            string? Text(string name) => current.GetProperty(name).ValueKind == JsonValueKind.Null ? null : current.GetProperty(name).GetString();

            return new Dictionary<string, object?>
            {
                ["yaasCode"] = Text("yaasCode"),
                ["owner"] = current.GetProperty("owner").GetInt32(),
                ["serialNumber"] = Text("serialNumber"),
                ["assetNumber"] = Text("assetNumber"),
                ["equipmentTypeId"] = Number("equipmentTypeId"),
                ["equipmentModelId"] = Number("equipmentModelId"),
                ["equipmentStatusId"] = Number("equipmentStatusId"),
                ["description"] = description ?? Text("description"),
                ["isActive"] = isActive ?? true,
                ["rowVersion"] = rowVersion,
            };
        }

        [Fact]
        public async Task Update_replaces_the_editable_fields_clears_what_is_not_sent_and_can_switch_between_type_and_model()
        {
            var c = await NewCatalogsAsync();
            var body = await BodyAsync(typeId: c.TypeId);
            body["serialNumber"] = "SN-1"; body["assetNumber"] = "AS-1"; body["description"] = "old";
            var created = await CreateAsync(body);

            var statusDamaged = (long)EquipmentStatusCode.Damaged;
            var update = new Dictionary<string, object?>
            {
                ["yaasCode"] = Uniq("Y2"), ["owner"] = 1, ["equipmentModelId"] = c.ModelId, ["equipmentStatusId"] = statusDamaged, ["isActive"] = true,
            };
            var response = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(created)}", update);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);

            Assert.Equal(JsonValueKind.Null, json.GetProperty("serialNumber").ValueKind);              // cleared
            Assert.Equal(JsonValueKind.Null, json.GetProperty("description").ValueKind);
            Assert.Equal(JsonValueKind.Null, json.GetProperty("equipmentTypeId").ValueKind);           // switched to a model
            Assert.Equal(c.ModelTitle, json.GetProperty("equipmentModelTitle").GetString());
            Assert.Equal("آسیب‌دیده", json.GetProperty("equipmentStatusTitle").GetString());
            Assert.NotEqual(created.GetProperty("rowVersion").GetString(), json.GetProperty("rowVersion").GetString());
        }

        [Fact]
        public async Task Update_of_an_unknown_id_is_404_and_the_location_is_not_changed_by_an_update()
        {
            var created = await CreateAsync(await BodyAsync());
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, "/api/Equipment/987654321", UpdateBody(created)), HttpStatusCode.NotFound, "not_found");

            var other = await NewLocationTypeAsync();
            var body = UpdateBody(created);
            body["currentEquipmentLocationTypeId"] = other;            // not part of the update: ignored
            var json = await ReadDataAsync(await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(created)}", body));
            Assert.Equal(created.GetProperty("currentEquipmentLocationTypeId").GetInt64(), json.GetProperty("currentEquipmentLocationTypeId").GetInt64());
        }

        [Fact]
        public async Task A_stale_row_version_is_a_409_concurrency_conflict()
        {
            var created = await CreateAsync(await BodyAsync());
            var first = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(created)}", UpdateBody(created, description: "one", rowVersion: created.GetProperty("rowVersion").GetString()));
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);

            // the same (now stale) version again
            var second = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(created)}", UpdateBody(created, description: "two", rowVersion: created.GetProperty("rowVersion").GetString()));
            await AssertErrorAsync(second, HttpStatusCode.Conflict, "concurrency_conflict");
        }

        [Fact]
        public async Task Any_status_change_is_allowed_there_is_no_transition_rule_yet()
        {
            var created = await CreateAsync(await BodyAsync(status: (long)EquipmentStatusCode.Scrapped));
            foreach (var status in Enum.GetValues<EquipmentStatusCode>().Where(s => s != EquipmentStatusCode.Unknown))
            {
                var body = UpdateBody(created);
                body["equipmentStatusId"] = (long)status;
                Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{Id(created)}", body)).StatusCode);
            }
        }

        // ------------------------------------------------------------------------------------------ soft delete

        [Fact]
        public async Task Delete_deactivates_hides_from_the_default_list_keeps_get_and_can_be_restored()
        {
            var created = await CreateAsync(await BodyAsync());
            var id = Id(created);
            var yaas = created.GetProperty("yaasCode").GetString()!;

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);   // idempotent

            Assert.DoesNotContain(id, Ids(await ListAsync($"yaasCode={yaas}")));
            Assert.Contains(id, Ids(await ListAsync($"yaasCode={yaas}&isActive=false")));
            var get = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
            Assert.False(get.GetProperty("isActive").GetBoolean());

            var restored = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", UpdateBody(get));
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
            Assert.Contains(id, Ids(await ListAsync($"yaasCode={yaas}")));
        }

        [Fact]
        public async Task Delete_of_an_unknown_id_is_404()
        {
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Delete, "/api/Equipment/987654321"), HttpStatusCode.NotFound, "not_found");
        }

        [Fact]
        public async Task A_deactivated_equipment_still_shows_its_names_even_when_its_catalog_rows_were_deactivated_later()
        {
            // a type with no models of its own, so that once the equipment is gone nothing active depends on it
            var created = await CreateAsync(await BodyAsync(typeId: await CatalogFixtures.NewTypeAsync(this)));
            var id = Id(created);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);

            // nothing active depends on the type any more, so it can be deactivated too
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentType/{created.GetProperty("equipmentTypeId").GetInt64()}")).StatusCode);

            var get = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
            Assert.False(string.IsNullOrEmpty(get.GetProperty("effectiveTypeTitle").GetString()));

            // ...and restoring the equipment is refused while its type is deactivated
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", UpdateBody(get)), HttpStatusCode.BadRequest, "parent_inactive");
        }

        [Fact]
        public async Task A_catalog_row_used_by_an_active_equipment_cannot_be_deactivated()
        {
            var created = await CreateAsync(await BodyAsync());
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentType/{created.GetProperty("equipmentTypeId").GetInt64()}"), HttpStatusCode.Conflict, "has_active_dependents");
        }

        // ------------------------------------------------------------------------------------------ move and history

        [Fact]
        public async Task Move_changes_the_location_records_history_and_a_move_to_the_same_place_does_nothing()
        {
            var created = await CreateAsync(await BodyAsync());
            var id = Id(created);
            var org = await NewOrganizationAsync();
            var dept = await NewDepartmentAsync();
            var tech = await NewTechnicianAsync();
            var newType = await NewLocationTypeAsync();

            var move = new Dictionary<string, object?>
            {
                ["currentEquipmentLocationTypeId"] = newType, ["currentOrganizationId"] = org.OrganizationId,
                ["currentSupportingDepartmentId"] = dept.DepartmentId, ["technicianUserId"] = tech.Id, ["description"] = "تحویل به تکنسین",
            };
            var response = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", move);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);
            Assert.Equal(newType, json.GetProperty("currentEquipmentLocationTypeId").GetInt64());
            Assert.Equal(org.Title, json.GetProperty("currentOrganizationTitle").GetString());
            Assert.Equal(dept.CityTitle, json.GetProperty("cityTitle").GetString());
            Assert.Equal(tech.Id, json.GetProperty("technicianUserId").GetInt64());

            // the same move again: no new history entry
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", move)).StatusCode);

            var history = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history"))).EnumerateArray().ToList();
            Assert.Equal(2, history.Count);                                                          // first placement + one move
            Assert.Equal("تحویل به تکنسین", history[0].GetProperty("description").GetString());      // newest first
            Assert.Equal(newType, history[0].GetProperty("equipmentLocationTypeId").GetInt64());
            Assert.Equal("ثبت اولیه تجهیز", history[1].GetProperty("description").GetString());
        }

        [Fact]
        public async Task Move_validates_its_references_and_the_equipment_and_honours_the_row_version()
        {
            var created = await CreateAsync(await BodyAsync());
            var id = Id(created);

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment/987654321/move",
                new { currentEquipmentLocationTypeId = await NewLocationTypeAsync() }), HttpStatusCode.NotFound, "not_found");
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move",
                new { currentEquipmentLocationTypeId = 987654321 }), HttpStatusCode.NotFound, "parent_not_found");
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move",
                new { currentEquipmentLocationTypeId = 0 })).StatusCode);

            // the version from before an edit is stale
            var stale = created.GetProperty("rowVersion").GetString();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", UpdateBody(created, description: "edited"))).StatusCode);
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move",
                new { currentEquipmentLocationTypeId = await NewLocationTypeAsync(), rowVersion = stale }), HttpStatusCode.Conflict, "concurrency_conflict");
        }

        [Fact]
        public async Task History_of_an_unknown_equipment_is_404_and_is_paged()
        {
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/987654321/location-history"), HttpStatusCode.NotFound, "not_found");

            var created = await CreateAsync(await BodyAsync());
            for (var i = 0; i < 2; i++)
                await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{Id(created)}/move", new { currentEquipmentLocationTypeId = await NewLocationTypeAsync() });

            var page = await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{Id(created)}/location-history?pageSize=2&pageNumber=2"));
            Assert.Equal(3, PaginationOf(page).GetProperty("totalCount").GetInt32());
            Assert.Single(DataOf(page).EnumerateArray());
        }

        // ------------------------------------------------------------------------------------------ list: filters, sorting, paging

        [Fact]
        public async Task The_list_filters_by_every_dimension_and_combines_with_and()
        {
            var c1 = await NewCatalogsAsync();
            var c2 = await NewCatalogsAsync();
            var d1 = await NewDepartmentAsync();
            var org1 = await NewOrganizationAsync();
            var tech1 = await NewTechnicianAsync();
            var status2 = (long)EquipmentStatusCode.InUse;

            var tag = Guid.NewGuid().ToString("N")[..8];
            var bodyA = await BodyAsync($"FA-{tag}-1", modelId: c1.ModelId);
            bodyA["currentSupportingDepartmentId"] = d1.DepartmentId; bodyA["currentOrganizationId"] = org1.OrganizationId; bodyA["technicianUserId"] = tech1.Id;
            bodyA["serialNumber"] = $"SER-{tag}";
            var a = await CreateAsync(bodyA);

            var bodyB = await BodyAsync($"FA-{tag}-2", typeId: c2.TypeId, status: status2);
            bodyB["assetNumber"] = $"AST-{tag}";
            var b = await CreateAsync(bodyB);

            var tagQuery = $"search=FA-{tag}";
            Assert.Equal(new[] { Id(a), Id(b) }.OrderBy(x => x), Ids(await ListAsync(tagQuery)).OrderBy(x => x));

            async Task AssertOnly(string query, params long[] expected) =>
                Assert.Equal(expected.OrderBy(x => x), Ids(await ListAsync($"{tagQuery}&{query}")).OrderBy(x => x));

            await AssertOnly($"statusId={InStock}", Id(a));
            await AssertOnly($"statusId={InStock}&statusId={status2}", Id(a), Id(b));                      // multi value
            await AssertOnly($"typeId={c1.TypeId}", Id(a));                                                // effective type through the model
            await AssertOnly($"typeId={c2.TypeId}", Id(b));                                                // the equipment's own type
            await AssertOnly($"categoryId={c1.CategoryId}", Id(a));
            await AssertOnly($"modelId={c1.ModelId}", Id(a));
            await AssertOnly($"brandId={c1.BrandId}", Id(a));
            await AssertOnly($"supportingDepartmentId={d1.DepartmentId}", Id(a));
            await AssertOnly($"cityId={d1.CityId}", Id(a));
            await AssertOnly($"provinceId={d1.ProvinceId}", Id(a));
            await AssertOnly($"organizationId={org1.OrganizationId}", Id(a));
            await AssertOnly($"fieldId={org1.FieldId}", Id(a));
            await AssertOnly($"organizationManagementId={org1.ManagementId}", Id(a));                      // derived through the field
            await AssertOnly($"zoneId={org1.ZoneId}", Id(a));
            await AssertOnly($"technicianUserId={tech1.Id}", Id(a));
            await AssertOnly($"serialNumber=SER-{tag}", Id(a));
            await AssertOnly($"assetNumber=AST-{tag}", Id(b));
            await AssertOnly($"locationTypeId={a.GetProperty("currentEquipmentLocationTypeId").GetInt64()}", Id(a));
            await AssertOnly($"statusId={InStock}&typeId={c2.TypeId}");                                    // AND: nothing

            // search also finds serial and asset numbers
            Assert.Equal(new[] { Id(a) }, Ids(await ListAsync($"search=SER-{tag}")));
            Assert.Equal(new[] { Id(b) }, Ids(await ListAsync($"search=AST-{tag}")));
        }

        [Fact]
        public async Task The_list_filters_by_creation_date_range_sorts_pages_and_has_the_item_shape()
        {
            var tag = Guid.NewGuid().ToString("N")[..8];
            var ids = new List<long>();
            foreach (var suffix in new[] { "B", "A", "C" })
                ids.Add(Id(await CreateAsync(await BodyAsync($"SO-{tag}-{suffix}"))));

            var search = $"search=SO-{tag}";
            Assert.Equal(new[] { $"SO-{tag}-A", $"SO-{tag}-B", $"SO-{tag}-C" },
                DataOf(await ListAsync($"{search}&sortBy=yaasCode")).EnumerateArray().Select(i => i.GetProperty("yaasCode").GetString()).ToArray());
            Assert.Equal(new[] { $"SO-{tag}-C", $"SO-{tag}-B", $"SO-{tag}-A" },
                DataOf(await ListAsync($"{search}&sortBy=yaasCode&sortDirection=desc")).EnumerateArray().Select(i => i.GetProperty("yaasCode").GetString()).ToArray());
            Assert.Equal(ids, Ids(await ListAsync(search)));                                               // default order: by id

            var second = await ListAsync($"{search}&sortBy=yaasCode&pageSize=2&pageNumber=2");
            Assert.Equal(new[] { $"SO-{tag}-C" }, DataOf(second).EnumerateArray().Select(i => i.GetProperty("yaasCode").GetString()).ToArray());
            Assert.Equal(3, PaginationOf(second).GetProperty("totalCount").GetInt32());

            var now = DateTime.UtcNow;
            Assert.Equal(3, Ids(await ListAsync($"{search}&createdFrom={now.AddHours(-1):O}&createdTo={now.AddHours(1):O}")).Count);
            Assert.Empty(Ids(await ListAsync($"{search}&createdTo={now.AddDays(-1):O}")));
            Assert.Empty(Ids(await ListAsync($"{search}&createdFrom={now.AddDays(1):O}")));

            var item = DataOf(second)[0];
            foreach (var name in new[] { "id", "yaasCode", "effectiveTypeTitle", "categoryTitle", "equipmentStatusTitle", "currentEquipmentLocationTypeTitle", "technicianName", "isActive", "createdDateTime" })
                Assert.True(item.TryGetProperty(name, out _), name);
            Assert.False(item.TryGetProperty("rowVersion", out _));                                         // the list item has no row version
        }

        [Fact]
        public async Task Invalid_list_parameters_are_validation_errors()
        {
            foreach (var query in new[] { "statusId=0", "typeId=-1", "createdFrom=2025-02-01T00:00:00Z&createdTo=2025-01-01T00:00:00Z", "pageSize=101", "pageNumber=0" })
                Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Get, $"/api/Equipment?{query}")).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ technicians

        [Fact]
        public async Task The_technician_lookup_lists_active_users_and_searches_by_name_or_personnel_code()
        {
            var tech = await NewTechnicianAsync();
            var inactive = await NewTechnicianAsync();
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                (await db.Users.SingleAsync(u => u.Id == inactive.Id)).IsActive = false;
                await db.SaveChangesAsync();
            }

            var all = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/technicians?pageSize=100"))).EnumerateArray().Select(Id).ToList();
            Assert.Contains(tech.Id, all);
            Assert.DoesNotContain(inactive.Id, all);

            var item = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/technicians?search=PC-tc_"))).EnumerateArray().First();
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("fullName").GetString()));
            Assert.StartsWith("PC-tc_", item.GetProperty("personnelCode").GetString());
            Assert.Empty(DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/technicians?search=zzzz-no-such"))).EnumerateArray());
        }

        // ------------------------------------------------------------------------------------------ Excel export

        [Fact]
        public async Task Export_returns_an_xlsx_with_persian_headers_jalali_dates_and_the_same_rows_as_the_list()
        {
            var c = await NewCatalogsAsync();
            var tag = Guid.NewGuid().ToString("N")[..8];
            var body = await BodyAsync($"XL-{tag}-1", modelId: c.ModelId);
            body["serialNumber"] = "000777";
            var a = await CreateAsync(body);
            await CreateAsync(await BodyAsync($"XL-{tag}-2", typeId: c.TypeId));
            var other = await CreateAsync(await BodyAsync($"OT-{tag}"));

            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/export?search=XL-{tag}&sortBy=yaasCode");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType!.MediaType);
            var fileName = response.Content.Headers.ContentDisposition!.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName!;
            Assert.Matches(@"^تجهیزات-\d{4}-\d{2}-\d{2}\.xlsx$", fileName);

            using var workbook = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            var sheet = workbook.Worksheet(1);
            Assert.Equal("تجهیزات", sheet.Name);
            Assert.True(sheet.RightToLeft);

            var headers = Enumerable.Range(1, sheet.LastColumnUsed()!.ColumnNumber()).Select(i => sheet.Cell(1, i).GetString()).ToList();
            foreach (var title in new[] { "شناسه", "یاس کد", "سریال نامبر", "دسته‌بندی", "نوع تجهیز", "مدل", "برند", "وضعیت", "نوع مکان", "سازمان", "شهر", "استان", "تکنسین", "فعال", "تاریخ ایجاد" })
                Assert.Contains(title, headers);

            string Cell(int row, string header) => sheet.Cell(row, headers.IndexOf(header) + 1).GetString();

            Assert.Equal(3, sheet.LastRowUsed()!.RowNumber());                                         // header + the two XL- rows, not the OT- one
            Assert.Equal($"XL-{tag}-1", Cell(2, "یاس کد"));
            Assert.Equal("000777", Cell(2, "سریال نامبر"));                                            // leading zeros kept (text)
            Assert.Equal(c.ModelTitle, Cell(2, "مدل"));
            Assert.Equal(c.BrandTitle, Cell(2, "برند"));
            Assert.Equal(c.CategoryTitle, Cell(2, "دسته‌بندی"));
            Assert.Equal("موجود در انبار", Cell(2, "وضعیت"));
            Assert.Equal("بله", Cell(2, "فعال"));
            Assert.DoesNotContain(Enumerable.Range(2, 2), r => Cell(r, "یاس کد") == $"OT-{tag}");

            // the date is the Jalali form of the very same UTC moment the API returns
            var createdUtc = DateTime.Parse(a.GetProperty("createdDateTime").GetString()!, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
            Assert.Equal(PersianDate.ToDateTime(createdUtc), Cell(2, "تاریخ ایجاد"));
            Assert.Matches(@"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}$", Cell(2, "تاریخ ایجاد"));

            // the export and the list agree on which rows match
            var listed = DataOf(await ListAsync($"search=XL-{tag}&sortBy=yaasCode")).EnumerateArray().Select(i => i.GetProperty("yaasCode").GetString()).ToList();
            Assert.Equal(listed, new[] { Cell(2, "یاس کد"), Cell(3, "یاس کد") });
        }

        [Fact]
        public async Task Export_of_a_filter_that_matches_nothing_is_a_valid_file_and_invalid_filters_are_json_errors()
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/export?search=nothing-{Guid.NewGuid():N}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var workbook = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            Assert.Equal(1, workbook.Worksheet(1).LastRowUsed()!.RowNumber());                          // just the header

            var bad = await SendAdminAsync(HttpMethod.Get, "/api/Equipment/export?statusId=0");
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(bad)));
        }

        // ------------------------------------------------------------------------------------------ authorization

        [Fact]
        public async Task Every_endpoint_needs_its_own_permission_and_anonymous_callers_get_401()
        {
            var admin = await AdminAsync();
            var created = await CreateAsync(await BodyAsync());
            var id = Id(created);

            async Task<string> TokenWithAsync(string name, params string[] permissions)
            {
                var userName = name + Guid.NewGuid().ToString("N")[..8];
                var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
                var roleId = await CreateRoleAsync(admin, "EqRole_" + userName, permissions);
                await AssignRoleAsync(admin, userId, roleId);
                return (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            }

            var nobody = await TokenWithAsync("eqn_", "Skills.View");
            var viewer = await TokenWithAsync("eqv_", "Equipments.View");
            var assigner = await TokenWithAsync("eqa_", "Equipments.Assign");

            foreach (var (method, url) in new[] { (HttpMethod.Get, "/api/Equipment"), (HttpMethod.Get, $"/api/Equipment/{id}"), (HttpMethod.Get, "/api/Equipment/export"),
                                                  (HttpMethod.Get, "/api/Equipment/technicians"), (HttpMethod.Get, $"/api/Equipment/{id}/location-history") })
            {
                Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(method, url, nobody));
                Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(method, url, viewer));
                Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(method, url)).StatusCode);
            }

            // viewing is not enough to change anything
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, "/api/Equipment", viewer, await BodyAsync()));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Put, $"/api/Equipment/{id}", viewer, UpdateBody(created)));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Delete, $"/api/Equipment/{id}", viewer));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", viewer, new { currentEquipmentLocationTypeId = 1 }));

            // moving needs Equipments.Assign (and nothing else)
            var moved = await StatusOfAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", assigner, new { currentEquipmentLocationTypeId = await NewLocationTypeAsync() });
            Assert.Equal(HttpStatusCode.OK, moved);
        }
    }
}
