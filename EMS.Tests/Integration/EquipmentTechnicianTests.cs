using EMS.Domain.Enums;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The technician of an equipment: any user can be one (ADR-0017: no post decides it, and only that the user exists is checked, in every
    /// create, bulk, restore and move); the dropdown lists the active users; GET /api/Equipment/{id}/technician shows the current one with his
    /// post and department.
    /// </summary>
    public class EquipmentTechnicianTests : ApiTestBase
    {
        public EquipmentTechnicianTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private static long LinePost => (long)PostCode.Line;
        private static long StaffPost => (long)PostCode.Staff;

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<long> NewUserAsync(long? postId, long? departmentId = null)
        {
            var userName = "tp" + Guid.NewGuid().ToString("N")[..10];
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Users", new
            {
                userName, password = "Passw0rd!x", firstName = "علی", lastName = "تکنسین" + Guid.NewGuid().ToString("N")[..4],
                personnelCode = "PC-" + userName, postId, supportingDepartmentIds = departmentId.HasValue ? new[] { departmentId.Value } : Array.Empty<long>(),
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task SetPostAsync(long userId, long? postId)
        {
            var user = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Users/{userId}"));
            var response = await SendAdminAsync(HttpMethod.Put, $"/api/Users/{userId}", new
            {
                userName = user.GetProperty("userName").GetString(), firstName = "علی", lastName = "تکنسین", personnelCode = user.GetProperty("personnelCode").GetString(),
                isActive = true, postId,
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<long> NewDepartmentAsync(string title)
        {
            var province = await CatalogFixtures.PostAsync(this, "/api/Province", new { title = Uniq("tpprov") }, "id");
            var city = await CatalogFixtures.PostAsync(this, "/api/City", new { provinceId = province, title = Uniq("tpcity") }, "id");
            return await CatalogFixtures.PostAsync(this, "/api/SupportingDepartment", new { cityId = city, title }, "id");
        }

        private sealed record Fixture(long TypeId, long DepartmentId);
        private async Task<Fixture> NewFixtureAsync()
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("tpcat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("tptype"), code = "T" }, "id");
            var location = await CatalogFixtures.NewDepartmentAsync(this);
            return new Fixture(type, location);
        }

        private static Dictionary<string, object?> Body(Fixture f, long? technician = null) => new()
        {
            ["yaasCode"] = Uniq("TPY"), ["owner"] = 1, ["equipmentTypeId"] = f.TypeId,
            ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock, ["currentSupportingDepartmentId"] = f.DepartmentId,
            ["technicianUserId"] = technician,
        };

        private async Task<long> NewEquipmentAsync(Fixture f, long? technician = null)
        {
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, technician));
            Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        // ------------------------------------------------------------------------------------ the lookup

        [Fact]
        public async Task The_lookup_lists_every_active_user_whatever_his_post()
        {
            var line = await NewUserAsync(LinePost);
            var staff = await NewUserAsync(StaffPost);
            var noPost = await NewUserAsync(null);
            var deactivated = await NewUserAsync(LinePost);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Users/{deactivated}")).StatusCode);

            var ids = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/Equipment/technicians?pageSize=100&search=PC-tp")))
                .EnumerateArray().Select(t => t.GetProperty("id").GetInt64()).ToHashSet();
            Assert.Contains(line, ids);
            Assert.Contains(staff, ids);
            Assert.Contains(noPost, ids);                  // the post is not a criterion
            Assert.DoesNotContain(deactivated, ids);       // the dropdown offers only active users
        }

        // ------------------------------------------------------------------------------------ assigning

        [Fact]
        public async Task Any_existing_user_can_be_the_technician_in_every_way_of_assigning_and_an_unknown_one_is_refused()
        {
            var f = await NewFixtureAsync();
            var staff = await NewUserAsync(StaffPost);
            var noPost = await NewUserAsync(null);
            var inactive = await NewUserAsync(LinePost);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Users/{inactive}")).StatusCode);

            // create: no post and not even an active user is required
            foreach (var user in new[] { staff, noPost, inactive })
            {
                var created = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, user));
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                Assert.Equal(user, (await ReadDataAsync(created)).GetProperty("technicianUserId").GetInt64());
            }

            // bulk
            Assert.Equal(HttpStatusCode.Created, (await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items = new[] { Body(f, staff), Body(f, noPost), Body(f, inactive) } })).StatusCode);

            // move
            var id = await NewEquipmentAsync(f);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { technicianUserId = noPost })).StatusCode);
            Assert.Equal(noPost, (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"))).GetProperty("technicianUserId").GetInt64());

            // only that the user exists is checked: unknown is 404 on create and move, and the bulk detail names the item
            var unknown = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, 987654321));
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));
            Assert.Equal(HttpStatusCode.NotFound, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { technicianUserId = 987654321 })).StatusCode);

            var bulk = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items = new[] { Body(f, staff), Body(f, 987654321) } });
            Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
            var details = (await ReadJsonAsync(bulk)).GetProperty("error").GetProperty("details").EnumerateArray().ToList();
            Assert.Single(details);
            Assert.Equal("items[1].technicianUserId", details[0].GetProperty("field").GetString());
            Assert.Contains("تکنسین", details[0].GetProperty("message").GetString());
        }

        [Fact]
        public async Task An_equipment_keeps_its_technician_when_his_post_changes_and_can_still_be_restored()
        {
            var f = await NewFixtureAsync();
            var technician = await NewUserAsync(LinePost);
            var id = await NewEquipmentAsync(f, technician);

            await SetPostAsync(technician, StaffPost);                                   // his post changed: it never mattered

            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
            Assert.Equal(technician, detail.GetProperty("technicianUserId").GetInt64());      // nothing is taken away
            Assert.Equal(technician, (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician"))).GetProperty("id").GetInt64());

            // an edit of the equipment does not re-check the technician; deactivating and restoring does not either
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);
            var restore = new Dictionary<string, object?>
            {
                ["yaasCode"] = detail.GetProperty("yaasCode").GetString(), ["owner"] = 1, ["equipmentTypeId"] = f.TypeId,
                ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock, ["isActive"] = true,
            };
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", restore)).StatusCode);
        }

        // ------------------------------------------------------------------------------------ the technician of one equipment

        [Fact]
        public async Task The_technician_endpoint_shows_the_person_with_his_post_and_department()
        {
            var f = await NewFixtureAsync();
            var departmentTitle = Uniq("tpdept");
            var department = await NewDepartmentAsync(departmentTitle);
            var technician = await NewUserAsync(LinePost, department);
            var id = await NewEquipmentAsync(f, technician);

            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);
            Assert.Equal(technician, json.GetProperty("id").GetInt64());
            Assert.StartsWith("علی تکنسین", json.GetProperty("fullName").GetString());
            Assert.StartsWith("PC-tp", json.GetProperty("personnelCode").GetString());
            Assert.True(json.GetProperty("isActive").GetBoolean());
            Assert.Equal(LinePost, json.GetProperty("postId").GetInt64());
            Assert.Equal("صف", json.GetProperty("postTitle").GetString());
            var departments = json.GetProperty("supportingDepartments").EnumerateArray().ToList();
            Assert.Equal(department, Assert.Single(departments).GetProperty("id").GetInt64());
            Assert.Equal(departmentTitle, departments[0].GetProperty("title").GetString());

            // no phone number or e-mail on purpose
            foreach (var name in new[] { "phoneNumber", "email", "nationalCode" })
                Assert.False(json.TryGetProperty(name, out _), name);
        }

        [Fact]
        public async Task The_technician_endpoint_answers_null_without_a_technician_and_404_for_an_unknown_equipment()
        {
            var f = await NewFixtureAsync();
            var id = await NewEquipmentAsync(f);

            var none = await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician");
            Assert.Equal(HttpStatusCode.OK, none.StatusCode);
            var json = await ReadJsonAsync(none);
            Assert.True(json.GetProperty("success").GetBoolean());
            Assert.Equal(JsonValueKind.Null, json.GetProperty("data").ValueKind);

            var unknown = await SendAdminAsync(HttpMethod.Get, "/api/Equipment/99999999/technician");
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));
        }

        [Fact]
        public async Task A_deactivated_technician_or_equipment_still_shows_and_the_endpoint_needs_the_view_permission()
        {
            var f = await NewFixtureAsync();
            var technician = await NewUserAsync(LinePost);
            var id = await NewEquipmentAsync(f, technician);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Users/{technician}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);

            var json = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician"));
            Assert.Equal(technician, json.GetProperty("id").GetInt64());
            Assert.False(json.GetProperty("isActive").GetBoolean());

            var admin = await AdminAsync();
            var userName = "tpplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/Equipment/{id}/technician")).StatusCode);
        }
    }
}
