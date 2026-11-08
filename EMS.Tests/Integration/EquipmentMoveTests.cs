using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// POST /api/Equipment/{id}/move is a PARTIAL change: every field is optional and "not sent" means "leave it". One call can change the
    /// technician only, the branch only, the department only, or the technician with one of the two places (ADR-0015).
    /// </summary>
    public class EquipmentMoveTests : ApiTestBase
    {
        public EquipmentMoveTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<long> NewUserAsync(long? postId = null) =>
            await CreateUserAsync(await AdminAsync(), "mv_" + Guid.NewGuid().ToString("N")[..10], "Passw0rd!x", postId: postId);

        private async Task<long> NewBranchAsync(bool active = true)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var tag = Guid.NewGuid().ToString("N")[..10];
            var zone = new Zone { Title = "z" + tag };
            var management = new OrganizationManagement { Title = "m" + tag, Zone = zone };
            var organization = new Organization { Title = "o" + tag, OrganizationManagement = management, IsActive = active };
            db.Add(organization);
            await db.SaveChangesAsync();
            return organization.OrganizationId;
        }

        private async Task<long> NewEquipmentAtDepartmentAsync(long? technician = null)
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("mvcat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("mvtype"), code = "T" }, "id");
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", new Dictionary<string, object?>
            {
                ["yaasCode"] = Uniq("MV"), ["owner"] = 1, ["equipmentTypeId"] = type, ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock,
                ["currentSupportingDepartmentId"] = await CatalogFixtures.NewDepartmentAsync(this), ["technicianUserId"] = technician,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<JsonElement> DetailAsync(long id) => await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
        private async Task<int> HistoryCountAsync(long id) =>
            DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history"))).GetArrayLength();

        private static long? Long(JsonElement e, string name) => e.GetProperty(name).ValueKind == JsonValueKind.Null ? null : e.GetProperty(name).GetInt64();

        [Fact]
        public async Task Only_the_technician_changes_when_only_the_technician_is_sent()
        {
            var technician = await NewUserAsync();
            var id = await NewEquipmentAtDepartmentAsync();
            var before = await DetailAsync(id);

            var response = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { technicianUserId = technician, description = "تحویل" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var after = await ReadDataAsync(response);

            Assert.Equal(technician, Long(after, "technicianUserId"));
            Assert.Equal(Long(before, "currentSupportingDepartmentId"), Long(after, "currentSupportingDepartmentId"));      // the place is untouched
            Assert.Null(Long(after, "currentOrganizationId"));
            Assert.Equal(2, await HistoryCountAsync(id));
            var newest = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history"))).EnumerateArray().First();
            Assert.Equal(technician, Long(newest, "technicianUserId"));                                                         // the entry is a snapshot of the whole state
            Assert.Equal(Long(before, "currentSupportingDepartmentId"), Long(newest, "currentSupportingDepartmentId"));
        }

        [Fact]
        public async Task Naming_a_branch_moves_there_and_leaves_the_department_and_the_other_way_round_and_the_technician_stays()
        {
            var technician = await NewUserAsync();
            var id = await NewEquipmentAtDepartmentAsync(technician);
            var branch = await NewBranchAsync();

            var toBranch = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentOrganizationId = branch }));
            Assert.Equal(branch, Long(toBranch, "currentOrganizationId"));
            Assert.Null(Long(toBranch, "currentSupportingDepartmentId"));        // left automatically
            Assert.Equal(1, toBranch.GetProperty("locationKind").GetInt32());
            Assert.Equal(technician, Long(toBranch, "technicianUserId"));         // not sent: unchanged

            var department = await CatalogFixtures.NewDepartmentAsync(this);
            var toDepartment = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentSupportingDepartmentId = department }));
            Assert.Equal(department, Long(toDepartment, "currentSupportingDepartmentId"));
            Assert.Null(Long(toDepartment, "currentOrganizationId"));
            Assert.Equal(2, toDepartment.GetProperty("locationKind").GetInt32());
            Assert.Equal(technician, Long(toDepartment, "technicianUserId"));
            Assert.Equal(3, await HistoryCountAsync(id));
        }

        [Fact]
        public async Task The_technician_and_one_place_can_change_in_one_call()
        {
            var first = await NewUserAsync();
            var second = await NewUserAsync();
            var id = await NewEquipmentAtDepartmentAsync(first);
            var branch = await NewBranchAsync();

            var after = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentOrganizationId = branch, technicianUserId = second }));
            Assert.Equal(branch, Long(after, "currentOrganizationId"));
            Assert.Equal(second, Long(after, "technicianUserId"));
            Assert.Equal(2, await HistoryCountAsync(id));
        }

        [Fact]
        public async Task The_technician_is_taken_off_only_by_asking_for_it_explicitly()
        {
            var technician = await NewUserAsync();
            var id = await NewEquipmentAtDepartmentAsync(technician);

            // null / not sent means "leave it": this asks for something else, the technician stays
            var other = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentSupportingDepartmentId = await CatalogFixtures.NewDepartmentAsync(this), technicianUserId = (long?)null }));
            Assert.Equal(technician, Long(other, "technicianUserId"));

            var cleared = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { clearTechnician = true, description = "برگشت" }));
            Assert.Null(Long(cleared, "technicianUserId"));
            Assert.Equal(2, cleared.GetProperty("locationKind").GetInt32());                  // the place did not change with it (still a department)

            // asking for the same again changes nothing
            var count = await HistoryCountAsync(id);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { clearTechnician = true })).StatusCode);
            Assert.Equal(count, await HistoryCountAsync(id));
        }

        [Fact]
        public async Task Contradictory_or_empty_requests_are_refused_with_persian_validation_errors()
        {
            var technician = await NewUserAsync();
            var id = await NewEquipmentAtDepartmentAsync();
            var department = await CatalogFixtures.NewDepartmentAsync(this);
            var branch = await NewBranchAsync();

            async Task AssertRefusedAsync(object body, string field)
            {
                var response = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var json = await ReadJsonAsync(response);
                Assert.Equal("validation_failed", ErrorCodeOf(json));
                Assert.Contains(field, ErrorFieldsOf(json));
                foreach (var d in json.GetProperty("error").GetProperty("details").EnumerateArray())
                    Assert.Matches("[؀-ۿ]", d.GetProperty("message").GetString()!);
            }

            await AssertRefusedAsync(new { currentOrganizationId = branch, currentSupportingDepartmentId = department }, "currentOrganizationId");   // exactly one place
            await AssertRefusedAsync(new { clearTechnician = true, technicianUserId = technician }, "clearTechnician");                          // set and take off
            await AssertRefusedAsync(new { description = "nothing to change" }, "technicianUserId");                                           // nothing asked
            await AssertRefusedAsync(new { }, "technicianUserId");
            Assert.Equal(1, await HistoryCountAsync(id));                                                                                       // none of them left a trace
        }

        [Fact]
        public async Task A_move_checks_that_the_targets_exist_but_not_that_they_are_active_or_what_post_the_technician_has()
        {
            var id = await NewEquipmentAtDepartmentAsync();

            // unknown ids are 404 (they would be database errors)
            foreach (var body in new object[] { new { currentOrganizationId = 987654321 }, new { currentSupportingDepartmentId = 987654321 }, new { technicianUserId = 987654321 } })
            {
                var response = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", body);
                Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(response)));
            }

            // no restriction: an inactive branch, an inactive department, a user without the technician post
            var inactiveBranch = await NewBranchAsync(active: false);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentOrganizationId = inactiveBranch })).StatusCode);

            var inactiveDepartment = await CatalogFixtures.NewDepartmentAsync(this);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{inactiveDepartment}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentSupportingDepartmentId = inactiveDepartment })).StatusCode);

            var noPost = await NewUserAsync(postId: null);
            var moved = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { technicianUserId = noPost });
            Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
            Assert.Equal(noPost, Long(await ReadDataAsync(moved), "technicianUserId"));
        }

        [Fact]
        public async Task A_move_to_the_state_it_is_already_in_does_nothing_and_the_row_version_still_guards_a_real_change()
        {
            var technician = await NewUserAsync();
            var id = await NewEquipmentAtDepartmentAsync(technician);
            var before = await DetailAsync(id);
            var stale = before.GetProperty("rowVersion").GetString();

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { technicianUserId = technician })).StatusCode);
            Assert.Equal(1, await HistoryCountAsync(id));                                       // no new entry

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { clearTechnician = true })).StatusCode);   // a real change
            var conflict = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { technicianUserId = technician, rowVersion = stale });
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("concurrency_conflict", ErrorCodeOf(await ReadJsonAsync(conflict)));
        }

        [Fact]
        public async Task A_deactivated_equipment_can_still_be_moved_and_an_unknown_one_is_404()
        {
            var id = await NewEquipmentAtDepartmentAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentOrganizationId = await NewBranchAsync() })).StatusCode);

            var missing = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/987654321/move", new { clearTechnician = true });
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal("not_found", ErrorCodeOf(await ReadJsonAsync(missing)));
        }
    }
}
