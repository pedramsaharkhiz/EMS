using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Dashboard cards. The numbers are compared with the lists they drill down to: every number must equal the totalCount of the matching
    /// list filter, and a card must count exactly what the list lets the caller see.
    /// </summary>
    public class DashboardTests : ApiTestBase
    {
        public DashboardTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private async Task<HttpResponseMessage> AsAdminAsync(HttpMethod method, string url, object? body = null) => await SendAsync(method, url, body, bearer: await AdminAsync());
        private Task<long> PostIdAsync(string url, object body) => CatalogFixtures.PostAsync(this, url, body, "id");
        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        private async Task<JsonElement> CardAsync(string card)
        {
            var response = await AsAdminAsync(HttpMethod.Get, $"/api/Dashboard/{card}");
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"{card} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadDataAsync(response);
        }

        /// <summary>The totalCount of a list URL (what a click on a dashboard number would show).</summary>
        private async Task<int> ListCountAsync(string url)
        {
            var response = await AsAdminAsync(HttpMethod.Get, url + (url.Contains('?') ? "&" : "?") + "pageSize=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return PaginationOf(await ReadJsonAsync(response)).GetProperty("totalCount").GetInt32();
        }

        private static JsonElement Bar(JsonElement card, string array, string codeOrId, bool byCode = true) =>
            card.GetProperty(array).EnumerateArray().First(b => byCode ? b.GetProperty("code").GetString() == codeOrId : b.GetProperty("id").GetInt64().ToString() == codeOrId);

        private static int CountOf(JsonElement card, string array, string code) => Bar(card, array, code).GetProperty("count").GetInt32();

        // ------------------------------------------------------------------------------------------ fixtures

        private long? _typeId;
        private long? _departmentId;

        private async Task<long> NewEquipmentAsync(EquipmentStatusCode status, EquipmentOwner owner, long? customStatusId = null)
        {
            _typeId ??= await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("dbcat") }), title = Uniq("dbtype"), code = "T" });
            _departmentId ??= await PostIdAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("dbdep") });
            var response = await AsAdminAsync(HttpMethod.Post, "/api/Equipment", new
            {
                yaasCode = Uniq("DBY"), owner = (int)owner, equipmentTypeId = _typeId, equipmentStatusId = customStatusId ?? (long)status, currentSupportingDepartmentId = _departmentId
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return Id(await ReadDataAsync(response));
        }

        private async Task<long> NewReportAsync(int priority, long? equipmentId = null)
        {
            var failureType = await PostIdAsync("/api/FailureType", new { failureCategoryId = await PostIdAsync("/api/FailureCategory", new { title = Uniq("dbfc") }), title = Uniq("dbft"), code = "FT" });
            long organizationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                var organization = new Organization
                {
                    Title = Uniq("dborg"),
                    Field = new Field { Title = Uniq("dbfield"), OrganizationManagement = new OrganizationManagement { Title = Uniq("dbmgmt"), Zone = new Zone { Title = Uniq("dbzone") } } },
                    OrganizationType = new OrganizationType { Title = Uniq("dbotype"), CreatedByUserId = 1 },
                };
                db.Organizations.Add(organization);
                await db.SaveChangesAsync();
                organizationId = organization.OrganizationId;
            }
            var report = await AsAdminAsync(HttpMethod.Post, "/api/FailureReport", new
            {
                equipmentId = equipmentId ?? await NewEquipmentAsync(EquipmentStatusCode.InUse, EquipmentOwner.Bank),
                failureTypeId = failureType, reportTypeId = (long)ReportTypeCode.Failure, organizationId, priorityId = priority, description = "خراب است"
            });
            Assert.Equal(HttpStatusCode.Created, report.StatusCode);
            return Id(await ReadDataAsync(report));
        }

        private async Task SetReportStatusAsync(long reportId, FailureReportStatusCode status)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var report = await db.FailureReports.IgnoreQueryFilters().FirstAsync(x => x.FailureReportId == reportId);
            report.FailureReportStatusId = (long)status;
            await db.SaveChangesAsync();
        }

        private async Task<long> AssignAsync(long reportId)
        {
            var response = await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{reportId}/assignments", new { technicianUserId = (await LoginAdminAsync()).UserId });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return Id(await ReadDataAsync(response));
        }

        // ------------------------------------------------------------------------------------------ equipment card

        [Fact]
        public async Task The_equipment_card_counts_active_equipment_per_status_and_owner_and_every_number_equals_its_list()
        {
            var before = await CardAsync("equipment");

            await NewEquipmentAsync(EquipmentStatusCode.InUse, EquipmentOwner.Bank);
            await NewEquipmentAsync(EquipmentStatusCode.UnderMaintenance, EquipmentOwner.Company);
            await NewEquipmentAsync(EquipmentStatusCode.Damaged, EquipmentOwner.Bank);
            await NewEquipmentAsync(EquipmentStatusCode.Lost, EquipmentOwner.Company);
            var removed = await NewEquipmentAsync(EquipmentStatusCode.OutOfService, EquipmentOwner.Bank);
            Assert.Equal(HttpStatusCode.OK, (await AsAdminAsync(HttpMethod.Delete, $"/api/Equipment/{removed}")).StatusCode);   // deactivated: not counted

            var after = await CardAsync("equipment");

            Assert.Equal(before.GetProperty("total").GetInt32() + 4, after.GetProperty("total").GetInt32());
            Assert.Equal(before.GetProperty("needsActionCount").GetInt32() + 2, after.GetProperty("needsActionCount").GetInt32());     // maintenance + damaged; lost is not
            Assert.Equal(CountOf(before, "byStatus", "InUse") + 1, CountOf(after, "byStatus", "InUse"));
            Assert.Equal(CountOf(before, "byStatus", "Lost") + 1, CountOf(after, "byStatus", "Lost"));
            Assert.Equal(CountOf(before, "byStatus", "OutOfService"), CountOf(after, "byStatus", "OutOfService"));

            // the sums always agree with the total
            Assert.Equal(after.GetProperty("total").GetInt32(), after.GetProperty("byStatus").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));
            Assert.Equal(after.GetProperty("total").GetInt32(), after.GetProperty("byOwner").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));

            // every system status has a bar, even with zero; the flags are exactly the three "needs action" ones
            var codes = after.GetProperty("byStatus").EnumerateArray().Where(b => b.GetProperty("needsAction").GetBoolean()).Select(b => b.GetProperty("code").GetString());
            Assert.Equal(new[] { "UnderMaintenance", "OutOfService", "Damaged" }.OrderBy(c => c), codes.OrderBy(c => c));
            foreach (var code in Enum.GetNames<EquipmentStatusCode>().Where(n => n != "Unknown"))
                Assert.Contains(after.GetProperty("byStatus").EnumerateArray(), b => b.GetProperty("code").GetString() == code);

            // each number is what the list says
            Assert.Equal(after.GetProperty("total").GetInt32(), await ListCountAsync("/api/Equipment"));
            foreach (var bar in after.GetProperty("byStatus").EnumerateArray())
                Assert.Equal(bar.GetProperty("count").GetInt32(), await ListCountAsync($"/api/Equipment?statusId={Id(bar)}"));
            Assert.Equal(after.GetProperty("needsActionCount").GetInt32(), await ListCountAsync("/api/Equipment?statusId=4&statusId=5&statusId=6"));
            foreach (var owner in after.GetProperty("byOwner").EnumerateArray())
                Assert.Equal(owner.GetProperty("count").GetInt32(), await ListCountAsync($"/api/Equipment?owner={owner.GetProperty("owner").GetInt32()}"));
            Assert.Equal(new[] { "بانک", "شرکت" }, after.GetProperty("byOwner").EnumerateArray().Select(o => o.GetProperty("title").GetString()));
        }

        [Fact]
        public async Task A_status_the_administrator_added_gets_a_bar_but_never_counts_as_needing_action()
        {
            var statusId = await PostIdAsync("/api/EquipmentStatus", new { title = Uniq("custom"), code = "CUSTOM" });
            var empty = Bar(await CardAsync("equipment"), "byStatus", statusId.ToString(), byCode: false);
            Assert.Equal(0, empty.GetProperty("count").GetInt32());                         // a bar with zero is still there
            Assert.False(empty.GetProperty("needsAction").GetBoolean());

            var before = await CardAsync("equipment");
            await NewEquipmentAsync(EquipmentStatusCode.InUse, EquipmentOwner.Bank, customStatusId: statusId);
            var after = await CardAsync("equipment");

            Assert.Equal(1, Bar(after, "byStatus", statusId.ToString(), byCode: false).GetProperty("count").GetInt32());
            Assert.Equal(before.GetProperty("total").GetInt32() + 1, after.GetProperty("total").GetInt32());
            Assert.Equal(before.GetProperty("needsActionCount").GetInt32(), after.GetProperty("needsActionCount").GetInt32());
            Assert.Equal(after.GetProperty("total").GetInt32(), after.GetProperty("byStatus").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));
        }

        // ------------------------------------------------------------------------------------------ failure report card

        [Fact]
        public async Task The_failure_report_card_counts_unresolved_reports_per_priority_and_those_without_an_assignment()
        {
            var before = await CardAsync("failure-reports");

            var urgent = await NewReportAsync(priority: 5);
            var plain = await NewReportAsync(priority: 3);
            var closed = await NewReportAsync(priority: 5);
            await SetReportStatusAsync(closed, FailureReportStatusCode.Closed);
            await AssignAsync(urgent);                                                      // urgent has an open assignment, plain has none

            var after = await CardAsync("failure-reports");

            Assert.Equal(before.GetProperty("total").GetInt32() + 3, after.GetProperty("total").GetInt32());
            Assert.Equal(before.GetProperty("unresolvedCount").GetInt32() + 2, after.GetProperty("unresolvedCount").GetInt32());
            Assert.Equal(before.GetProperty("unresolvedWithoutAssignmentCount").GetInt32() + 1, after.GetProperty("unresolvedWithoutAssignmentCount").GetInt32());   // only "plain"
            Assert.Equal(CountOf(before, "byStatus", "Closed") + 1, CountOf(after, "byStatus", "Closed"));

            // all five priorities, in order, with Persian titles; the closed urgent report is not among the unresolved
            var priorities = after.GetProperty("unresolvedByPriority").EnumerateArray().ToList();
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, priorities.Select(p => p.GetProperty("id").GetInt32()));
            Assert.All(priorities, p => Assert.False(string.IsNullOrWhiteSpace(p.GetProperty("title").GetString())));
            var beforeP = before.GetProperty("unresolvedByPriority").EnumerateArray().ToList();
            Assert.Equal(beforeP[4].GetProperty("count").GetInt32() + 1, priorities[4].GetProperty("count").GetInt32());
            Assert.Equal(beforeP[2].GetProperty("count").GetInt32() + 1, priorities[2].GetProperty("count").GetInt32());

            Assert.Equal(after.GetProperty("total").GetInt32(), after.GetProperty("byStatus").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));
            Assert.Equal(after.GetProperty("unresolvedCount").GetInt32(), priorities.Sum(p => p.GetProperty("count").GetInt32()));

            // each number is what the list says
            var unresolvedIds = string.Join("&", after.GetProperty("byStatus").EnumerateArray().Where(b => b.GetProperty("isUnresolved").GetBoolean()).Select(b => $"statusId={Id(b)}"));
            Assert.Equal(after.GetProperty("total").GetInt32(), await ListCountAsync("/api/FailureReport"));
            Assert.Equal(after.GetProperty("unresolvedCount").GetInt32(), await ListCountAsync($"/api/FailureReport?{unresolvedIds}"));
            Assert.Equal(after.GetProperty("unresolvedWithoutAssignmentCount").GetInt32(), await ListCountAsync($"/api/FailureReport?{unresolvedIds}&hasOpenAssignment=false"));
            foreach (var bar in after.GetProperty("byStatus").EnumerateArray())
                Assert.Equal(bar.GetProperty("count").GetInt32(), await ListCountAsync($"/api/FailureReport?statusId={Id(bar)}"));
            foreach (var p in priorities)
                Assert.Equal(p.GetProperty("count").GetInt32(), await ListCountAsync($"/api/FailureReport?{unresolvedIds}&priorityId={p.GetProperty("id").GetInt32()}"));
        }

        [Fact]
        public async Task The_hasOpenAssignment_filter_and_field_tell_apart_reports_with_and_without_open_work()
        {
            var tag = Uniq("HA");
            var assigned = await NewReportAsync(priority: 2);
            var free = await NewReportAsync(priority: 2);
            var assignmentId = await AssignAsync(assigned);

            async Task<bool?> FlagAsync(long reportId) =>
                (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{reportId}"))).GetProperty("hasOpenAssignment").GetBoolean();

            Assert.True(await FlagAsync(assigned));
            Assert.False(await FlagAsync(free));

            async Task<List<long>> IdsAsync(string q) =>
                (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport?pageSize=100&sortDirection=desc&{q}"))).EnumerateArray().Select(Id).ToList();

            Assert.Contains(assigned, await IdsAsync("hasOpenAssignment=true"));
            Assert.DoesNotContain(free, await IdsAsync("hasOpenAssignment=true"));
            Assert.Contains(free, await IdsAsync("hasOpenAssignment=false"));
            Assert.DoesNotContain(assigned, await IdsAsync("hasOpenAssignment=false"));

            // a finished assignment no longer counts as open work
            var cancel = await AsAdminAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{assignmentId}/cancel", new { note = tag });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            Assert.False(await FlagAsync(assigned));
        }

        // ------------------------------------------------------------------------------------------ assignment card

        [Fact]
        public async Task The_assignment_card_counts_open_and_overdue_assignments_and_every_number_equals_its_list()
        {
            var before = await CardAsync("assignments");

            var first = await AssignAsync(await NewReportAsync(priority: 1));
            var second = await AssignAsync(await NewReportAsync(priority: 1));
            var third = await AssignAsync(await NewReportAsync(priority: 1));
            Assert.Equal(HttpStatusCode.OK, (await AsAdminAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{third}/cancel", new { note = "x" })).StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                (await db.TechnicianAssignments.FirstAsync(x => x.TechnicianAssignmentId == first)).DeadlineDateTime = DateTime.UtcNow.AddHours(-3);     // open and late
                (await db.TechnicianAssignments.FirstAsync(x => x.TechnicianAssignmentId == third)).DeadlineDateTime = DateTime.UtcNow.AddHours(-3);     // late, but cancelled
                await db.SaveChangesAsync();
            }

            var after = await CardAsync("assignments");

            Assert.Equal(before.GetProperty("total").GetInt32() + 3, after.GetProperty("total").GetInt32());
            Assert.Equal(before.GetProperty("openCount").GetInt32() + 2, after.GetProperty("openCount").GetInt32());
            Assert.Equal(before.GetProperty("overdueCount").GetInt32() + 1, after.GetProperty("overdueCount").GetInt32());
            Assert.Equal(CountOf(before, "byStatus", "Cancelled") + 1, CountOf(after, "byStatus", "Cancelled"));
            Assert.Equal(new[] { "Assigned", "Accepted", "Rejected", "InProgress", "Completed", "Cancelled" }, after.GetProperty("byStatus").EnumerateArray().Select(b => b.GetProperty("code").GetString()));
            Assert.Equal(after.GetProperty("total").GetInt32(), after.GetProperty("byStatus").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));
            Assert.Equal(after.GetProperty("openCount").GetInt32(), after.GetProperty("byStatus").EnumerateArray().Where(b => b.GetProperty("isOpen").GetBoolean()).Sum(b => b.GetProperty("count").GetInt32()));

            Assert.Equal(after.GetProperty("total").GetInt32(), await ListCountAsync("/api/TechnicianAssignment"));
            Assert.Equal(after.GetProperty("overdueCount").GetInt32(), await ListCountAsync("/api/TechnicianAssignment?overdue=true"));
            foreach (var bar in after.GetProperty("byStatus").EnumerateArray())
                Assert.Equal(bar.GetProperty("count").GetInt32(), await ListCountAsync($"/api/TechnicianAssignment?statusId={Id(bar)}"));
            _ = second;
        }

        // ------------------------------------------------------------------------------------------ permissions

        [Fact]
        public async Task Each_card_needs_the_permission_of_the_data_it_counts()
        {
            var admin = await AdminAsync();
            async Task<string> TokenWithAsync(params string[] permissions)
            {
                var name = "db_" + Guid.NewGuid().ToString("N")[..10];
                var userId = await CreateUserAsync(admin, name, "Passw0rd!x");
                await AssignRoleAsync(admin, userId, await CreateRoleAsync(admin, "role_" + name, permissions));
                return (await LoginAsync(name, "Passw0rd!x")).AccessToken;
            }

            var equipmentOnly = await TokenWithAsync("Equipments.View");
            var reportsOnly = await TokenWithAsync("FailureReports.View");
            var assignmentsOnly = await TokenWithAsync("TechnicianAssignments.View");

            var cards = new (string Url, string Token)[]
            {
                ("/api/Dashboard/equipment", equipmentOnly),
                ("/api/Dashboard/failure-reports", reportsOnly),
                ("/api/Dashboard/assignments", assignmentsOnly),
            };
            foreach (var (url, owner) in cards)
                foreach (var (_, token) in cards)
                    Assert.Equal(token == owner ? HttpStatusCode.OK : HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, url, token));

            foreach (var (url, _) in cards)
                Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, url)).StatusCode);
        }
    }
}
