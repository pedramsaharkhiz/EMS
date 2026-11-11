using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>POST /api/Equipment/bulk-move: the change of a single move (partial, ADR-0015) for many equipment at once, all or nothing (ADR-0016).</summary>
    public class EquipmentBulkMoveTests : ApiTestBase
    {
        public EquipmentBulkMoveTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<long> NewUserAsync(long? postId = null) =>
            await CreateUserAsync(await AdminAsync(), "bm_" + Guid.NewGuid().ToString("N")[..10], "Passw0rd!x", postId: postId);

        private async Task<long> NewBranchAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var tag = Guid.NewGuid().ToString("N")[..10];
            var organization = new Organization { Title = "o" + tag, OrganizationManagement = new OrganizationManagement { Title = "m" + tag, Zone = new Zone { Title = "z" + tag } } };
            db.Add(organization);
            await db.SaveChangesAsync();
            return organization.OrganizationId;
        }

        private async Task<long> NewEquipmentAsync(long? department = null, long? technician = null)
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("bmcat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("bmtype"), code = "T" }, "id");
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", new Dictionary<string, object?>
            {
                ["yaasCode"] = Uniq("BM"), ["owner"] = 1, ["equipmentTypeId"] = type, ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock,
                ["currentSupportingDepartmentId"] = department ?? await CatalogFixtures.NewDepartmentAsync(this), ["technicianUserId"] = technician,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<JsonElement> DetailAsync(long id) => await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}"));
        private async Task<int> HistoryCountAsync(long id) =>
            DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history"))).GetArrayLength();
        private static long? Long(JsonElement e, string name) => e.GetProperty(name).ValueKind == JsonValueKind.Null ? null : e.GetProperty(name).GetInt64();

        [Fact]
        public async Task The_same_technician_goes_to_all_of_them_and_each_keeps_its_own_place()
        {
            var technician = await NewUserAsync();
            var a = await NewEquipmentAsync();
            var b = await NewEquipmentAsync();
            var placeA = Long(await DetailAsync(a), "currentSupportingDepartmentId");
            var placeB = Long(await DetailAsync(b), "currentSupportingDepartmentId");

            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, b }, technicianUserId = technician, description = "تخصیص گروهی" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await ReadDataAsync(response);
            Assert.Equal(2, result.GetProperty("selected").GetInt32());
            Assert.Equal(2, result.GetProperty("changed").GetInt32());
            Assert.Equal(0, result.GetProperty("unchanged").GetInt32());
            Assert.Equal(new[] { a, b }.OrderBy(x => x), result.GetProperty("changedIds").EnumerateArray().Select(x => x.GetInt64()).OrderBy(x => x));

            foreach (var (id, place) in new[] { (a, placeA), (b, placeB) })
            {
                var detail = await DetailAsync(id);
                Assert.Equal(technician, Long(detail, "technicianUserId"));
                Assert.Equal(place, Long(detail, "currentSupportingDepartmentId"));          // untouched
                Assert.Equal(2, await HistoryCountAsync(id));
                var newest = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Equipment/{id}/location-history"))).EnumerateArray().First();
                Assert.Equal("تخصیص گروهی", newest.GetProperty("description").GetString());
            }
        }

        [Fact]
        public async Task A_branch_refuses_retained_technicians_and_explicit_clearing_moves_every_equipment()
        {
            var technician = await NewUserAsync();
            var a = await NewEquipmentAsync(technician: technician);
            var b = await NewEquipmentAsync(technician: technician);
            var branch = await NewBranchAsync();

            var refused = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, b }, currentOrganizationId = branch });
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Equal("bulk_items_invalid", ErrorCodeOf(await ReadJsonAsync(refused)));
            Assert.Equal(technician, Long(await DetailAsync(a), "technicianUserId"));
            Assert.Equal(technician, Long(await DetailAsync(b), "technicianUserId"));
            var moved = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, b }, currentOrganizationId = branch, clearTechnician = true }));
            Assert.Equal(2, moved.GetProperty("changed").GetInt32());
            foreach (var id in new[] { a, b })
            {
                var detail = await DetailAsync(id);
                Assert.Equal(branch, Long(detail, "currentOrganizationId"));
                Assert.Null(Long(detail, "currentSupportingDepartmentId"));       // left automatically
                Assert.Null(Long(detail, "technicianUserId"));
            }

            var cleared = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, b }, clearTechnician = true }));
            Assert.Equal(0, cleared.GetProperty("changed").GetInt32());
            Assert.Null(Long(await DetailAsync(a), "technicianUserId"));
            Assert.Null(Long(await DetailAsync(b), "technicianUserId"));
        }

        [Fact]
        public async Task An_equipment_already_in_the_asked_state_is_counted_as_unchanged_and_gets_no_history_entry()
        {
            var technician = await NewUserAsync();
            var already = await NewEquipmentAsync(technician: technician);
            var other = await NewEquipmentAsync();

            var result = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { already, other }, technicianUserId = technician }));
            Assert.Equal(2, result.GetProperty("selected").GetInt32());
            Assert.Equal(1, result.GetProperty("changed").GetInt32());
            Assert.Equal(1, result.GetProperty("unchanged").GetInt32());
            Assert.Equal(new[] { other }, result.GetProperty("changedIds").EnumerateArray().Select(x => x.GetInt64()).ToArray());
            Assert.Equal(1, await HistoryCountAsync(already));
            Assert.Equal(2, await HistoryCountAsync(other));

            // nothing to do for any of them is still a success
            var again = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { already, other }, technicianUserId = technician }));
            Assert.Equal(0, again.GetProperty("changed").GetInt32());
        }

        [Fact]
        public async Task One_unknown_equipment_or_destination_refuses_everything_and_changes_nothing()
        {
            var technician = await NewUserAsync();
            var a = await NewEquipmentAsync();
            var b = await NewEquipmentAsync();

            // an unknown equipment: the detail names its position
            var unknown = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new long[] { a, 987654321, b }, technicianUserId = technician });
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
            var json = await ReadJsonAsync(unknown);
            Assert.Equal("bulk_items_invalid", ErrorCodeOf(json));
            Assert.Contains("equipmentIds[1]", ErrorFieldsOf(json));
            foreach (var d in json.GetProperty("error").GetProperty("details").EnumerateArray())
                Assert.Matches("[؀-ۿ]", d.GetProperty("message").GetString()!);

            // an unknown destination
            var noBranch = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, b }, currentOrganizationId = 987654321 });
            Assert.Equal(HttpStatusCode.NotFound, noBranch.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(noBranch)));
            var noTechnician = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, b }, technicianUserId = 987654321 });
            Assert.Equal(HttpStatusCode.NotFound, noTechnician.StatusCode);

            foreach (var id in new[] { a, b })
            {
                Assert.Null(Long(await DetailAsync(id), "technicianUserId"));
                Assert.Equal(1, await HistoryCountAsync(id));
            }
        }

        [Fact]
        public async Task The_destination_has_the_rules_of_a_single_move_no_active_or_post_restriction_and_deactivated_equipment_can_move()
        {
            var noPost = await NewUserAsync(postId: null);
            var a = await NewEquipmentAsync();
            var inactive = await NewEquipmentAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{inactive}")).StatusCode);

            var result = await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { a, inactive }, technicianUserId = noPost }));
            Assert.Equal(2, result.GetProperty("changed").GetInt32());
            Assert.Equal(noPost, Long(await DetailAsync(inactive), "technicianUserId"));
        }

        [Fact]
        public async Task A_bad_request_is_a_persian_validation_error_and_changes_nothing()
        {
            var a = await NewEquipmentAsync();
            var branch = await NewBranchAsync();
            var department = await CatalogFixtures.NewDepartmentAsync(this);
            var technician = await NewUserAsync();

            async Task AssertRefusedAsync(object body, string field)
            {
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk-move", body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var json = await ReadJsonAsync(response);
                Assert.Equal("validation_failed", ErrorCodeOf(json));
                Assert.Contains(field, ErrorFieldsOf(json).Select(f => f.Split('[')[0]));
                foreach (var d in json.GetProperty("error").GetProperty("details").EnumerateArray())
                    Assert.Matches("[؀-ۿ]", d.GetProperty("message").GetString()!);
            }

            await AssertRefusedAsync(new { technicianUserId = technician }, "equipmentIds");                                                       // no list
            await AssertRefusedAsync(new { equipmentIds = Array.Empty<long>(), technicianUserId = technician }, "equipmentIds");                    // empty list
            await AssertRefusedAsync(new { equipmentIds = Enumerable.Range(1, 201).Select(i => (long)i).ToArray(), technicianUserId = technician }, "equipmentIds");  // more than 200
            await AssertRefusedAsync(new { equipmentIds = new[] { a, a }, technicianUserId = technician }, "equipmentIds");                       // repeated
            await AssertRefusedAsync(new { equipmentIds = new long[] { 0 }, technicianUserId = technician }, "equipmentIds");                     // not an id
            await AssertRefusedAsync(new { equipmentIds = new[] { a }, currentOrganizationId = branch, currentSupportingDepartmentId = department }, "currentOrganizationId");
            await AssertRefusedAsync(new { equipmentIds = new[] { a }, clearTechnician = true, technicianUserId = technician }, "clearTechnician");
            await AssertRefusedAsync(new { equipmentIds = new[] { a }, description = "nothing asked" }, "technicianUserId");
            Assert.Equal(1, await HistoryCountAsync(a));
        }

        [Fact]
        public async Task It_needs_the_assign_permission_and_a_login()
        {
            var a = await NewEquipmentAsync();
            var admin = await AdminAsync();

            async Task<string> TokenWithAsync(string name, params string[] permissions)
            {
                var userName = name + Guid.NewGuid().ToString("N")[..8];
                var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
                var roleId = await CreateRoleAsync(admin, "BmRole_" + userName, permissions);
                await AssignRoleAsync(admin, userId, roleId);
                return (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            }

            var body = new { equipmentIds = new[] { a }, clearTechnician = true };
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Post, "/api/Equipment/bulk-move", body)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, "/api/Equipment/bulk-move", await TokenWithAsync("bmv_", "Equipments.View", "Equipments.Update"), body));
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Post, "/api/Equipment/bulk-move", await TokenWithAsync("bma_", "Equipments.Assign"), body));
        }
    }
}
