using ClosedXML.Excel;
using EMS.Application.Common.DataAccess;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Security.RowSecurity;
using EMS.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Row-level security end to end, with the real rule (BUSINESS-DECISIONS 3.23): a user sees the data of HIS supporting department and the work he was assigned;
    /// whoever holds DataScope.ViewAll (and the Admin) sees everything; a user without a department sees only his assigned work. The same rule limits the lists, the
    /// Excel files, the dashboard, the detail by id, every write, the destinations of writes, the attachments, the users and the technician choices; a hidden record
    /// answers exactly like a missing one.
    /// </summary>
    public class RowSecurityPipelineTests : ApiTestBase
    {
        public RowSecurityPipelineTests(EmsApiFactory factory) : base(factory) { }

        // here the roles hold exactly the permissions they name: who sees which department is what is being tested
        protected override bool RolesSeeEveryDepartment => false;

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private Task<HttpResponseMessage> As(string token, HttpMethod method, string url, object? body = null) => SendAsync(method, url, body, bearer: token);
        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();
        private Task<long> PostIdAsync(string url, object body) => CatalogFixtures.PostAsync(this, url, body, "id");

        private static readonly string[] FieldPermissions =
        {
            "Equipments.View", "Equipments.Create", "Equipments.Update", "Equipments.Delete", "Equipments.Assign",
            "FailureReports.View", "FailureReports.Create", "FailureReports.Update", "FailureReports.Delete",
            "TechnicianAssignments.View", "TechnicianAssignments.Assign", "TechnicianAssignments.Work",
            "Users.View", "Users.Create", "Users.Update", "Users.Delete", "Users.ManageRoles", "Roles.View", "Roles.ManageUsers",
        };

        private sealed record Person(long Id, string Token, string Code);

        private sealed class World
        {
            public long DepartmentA, DepartmentB, OrganizationA, OrganizationB, OrganizationNone;
            public long EquipmentA, EquipmentOrgA, EquipmentB, EquipmentNone;
            public long ReportA, ReportB, ReportNone;
            public Person UserA = null!, UserB = null!, NoDepartment = null!, Central = null!;
            public long TypeId, FailureTypeId;
        }

        private Task<Person> NewPersonAsync(long? department, params string[] permissions) =>
            NewPersonInAsync(department.HasValue ? new[] { department.Value } : Array.Empty<long>(), permissions);

        private async Task<Person> NewPersonInAsync(long[] departments, params string[] permissions)
        {
            var admin = await AdminAsync();
            var name = "rs_" + Guid.NewGuid().ToString("N")[..10];
            var created = await As(admin, HttpMethod.Post, "/api/Users", new
            {
                userName = name, password = "Passw0rd!x", firstName = "علی", lastName = "رضایی", personnelCode = "PC-" + name, supportingDepartmentIds = departments,
            });
            Assert.True(created.IsSuccessStatusCode, await created.Content.ReadAsStringAsync());
            var id = Id(await ReadDataAsync(created));
            await AssignRoleAsync(admin, id, await CreateRoleAsync(admin, "role_" + name, permissions));
            return new Person(id, (await LoginAsync(name, "Passw0rd!x")).AccessToken, "PC-" + name);
        }

        private async Task<long> NewOrganizationAsync(long? departmentId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var organization = new Organization
            {
                Title = Uniq("rsorg"),
                SupportingDepartmentId = departmentId,
                Field = new Field { Title = Uniq("rsfield"), OrganizationManagement = new OrganizationManagement { Title = Uniq("rsmgmt"), Zone = new Zone { Title = Uniq("rszone") } } },
                OrganizationType = new OrganizationType { Title = Uniq("rsotype"), CreatedByUserId = 1 },
            };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
            return organization.OrganizationId;
        }

        private async Task<long> EquipmentAsync(string token, World w, object holder, HttpStatusCode expected = HttpStatusCode.Created)
        {
            var body = new Dictionary<string, object?>
            {
                ["yaasCode"] = Uniq("RSY"), ["owner"] = 2, ["equipmentTypeId"] = w.TypeId, ["equipmentStatusId"] = (long)EquipmentStatusCode.UnderMaintenance,
            };
            foreach (var p in holder.GetType().GetProperties())
                body[char.ToLowerInvariant(p.Name[0]) + p.Name[1..]] = p.GetValue(holder);
            var response = await As(token, HttpMethod.Post, "/api/Equipment", body);
            Assert.True(response.StatusCode == expected, $"equipment -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return expected == HttpStatusCode.Created ? Id(await ReadDataAsync(response)) : 0;
        }

        private async Task<long> ReportAsync(string token, World w, long organization, long equipment, HttpStatusCode expected = HttpStatusCode.Created)
        {
            var response = await As(token, HttpMethod.Post, "/api/FailureReport", new
            {
                equipmentId = equipment, failureTypeId = w.FailureTypeId, reportTypeId = (long)ReportTypeCode.Failure, organizationId = organization, priorityId = 3, referenceNumber = Uniq("RSR"),
            });
            Assert.True(response.StatusCode == expected, $"report -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return expected == HttpStatusCode.Created ? Id(await ReadDataAsync(response)) : 0;
        }

        private async Task<World> NewWorldAsync()
        {
            var admin = await AdminAsync();
            var w = new World();
            var city = await CatalogFixtures.EnsureCityAsync(this);
            w.DepartmentA = await PostIdAsync("/api/SupportingDepartment", new { cityId = city, title = Uniq("rsA") });
            w.DepartmentB = await PostIdAsync("/api/SupportingDepartment", new { cityId = city, title = Uniq("rsB") });
            w.OrganizationA = await NewOrganizationAsync(w.DepartmentA);
            w.OrganizationB = await NewOrganizationAsync(w.DepartmentB);
            w.OrganizationNone = await NewOrganizationAsync(null);
            w.TypeId = await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("rsc") }), title = Uniq("rst"), code = "T" });
            w.FailureTypeId = await PostIdAsync("/api/FailureType", new { failureCategoryId = await PostIdAsync("/api/FailureCategory", new { title = Uniq("rsfc") }), title = Uniq("rsft"), code = "FT" });

            w.UserA = await NewPersonAsync(w.DepartmentA, FieldPermissions);
            w.UserB = await NewPersonAsync(w.DepartmentB, FieldPermissions);
            w.NoDepartment = await NewPersonAsync(null, FieldPermissions);
            w.Central = await NewPersonAsync(w.DepartmentB, FieldPermissions.Append("DataScope.ViewAll").ToArray());

            w.EquipmentA = await EquipmentAsync(admin, w, new { CurrentSupportingDepartmentId = w.DepartmentA });
            w.EquipmentOrgA = await EquipmentAsync(admin, w, new { CurrentOrganizationId = w.OrganizationA });
            w.EquipmentB = await EquipmentAsync(admin, w, new { CurrentSupportingDepartmentId = w.DepartmentB });
            w.EquipmentNone = await EquipmentAsync(admin, w, new { CurrentOrganizationId = w.OrganizationNone });
            w.ReportA = await ReportAsync(admin, w, w.OrganizationA, w.EquipmentA);
            w.ReportB = await ReportAsync(admin, w, w.OrganizationB, w.EquipmentB);
            w.ReportNone = await ReportAsync(admin, w, w.OrganizationNone, w.EquipmentNone);
            return w;
        }

        private async Task<List<long>> IdsAsync(string token, string url)
        {
            var response = await As(token, HttpMethod.Get, url + (url.Contains('?') ? "&" : "?") + "pageSize=100&sortDirection=desc");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ReadDataAsync(response)).EnumerateArray().Select(Id).ToList();
        }

        private async Task<int> TotalAsync(string token, string url)
        {
            var response = await As(token, HttpMethod.Get, url + (url.Contains('?') ? "&" : "?") + "pageSize=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return PaginationOf(await ReadJsonAsync(response)).GetProperty("totalCount").GetInt32();
        }

        /// <summary>A hidden record must answer exactly like one that does not exist: the same status, code and message.</summary>
        private async Task AssertLooksMissingAsync(string token, HttpMethod method, string hiddenUrl, string missingUrl, object? body = null)
        {
            var hidden = await As(token, method, hiddenUrl, body);
            var missing = await As(token, method, missingUrl, body);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
            var h = await ReadJsonAsync(hidden);
            var m = await ReadJsonAsync(missing);
            Assert.Equal(ErrorCodeOf(m), ErrorCodeOf(h));
            Assert.Equal(m.GetProperty("message").GetString(), h.GetProperty("message").GetString());
        }

        // ------------------------------------------------------------------------------------------ scope

        [Fact]
        public async Task My_scope_says_who_sees_what_and_why()
        {
            var w = await NewWorldAsync();

            var admin = await ReadDataAsync(await As(await AdminAsync(), HttpMethod.Get, "/api/RowSecurity/my-scope"));
            Assert.True(admin.GetProperty("isUnrestricted").GetBoolean());
            Assert.Contains("همه‌ی ستادها", admin.GetProperty("reason").GetString());

            var central = await ReadDataAsync(await As(w.Central.Token, HttpMethod.Get, "/api/RowSecurity/my-scope"));
            Assert.True(central.GetProperty("isUnrestricted").GetBoolean());

            var userA = await ReadDataAsync(await As(w.UserA.Token, HttpMethod.Get, "/api/RowSecurity/my-scope"));
            Assert.False(userA.GetProperty("isUnrestricted").GetBoolean());
            Assert.Equal(new[] { w.DepartmentA }, userA.GetProperty("supportingDepartmentIds").EnumerateArray().Select(x => x.GetInt64()).ToArray());
            Assert.Contains("ستاد شما", userA.GetProperty("reason").GetString());
            var policies = userA.GetProperty("policies").EnumerateArray().ToList();
            Assert.All(policies, p => Assert.True(p.GetProperty("restrictsYou").GetBoolean()));      // every protected type now has a real rule
            foreach (var type in new[] { "EquipmentRow", "FailureReportRow", "TechnicianAssignmentRow", "ApplicationUser", "UserDirectoryRow", "TechnicianInfo" })
                Assert.Contains(policies, p => p.GetProperty("rowType").GetString() == type);

            var none = await ReadDataAsync(await As(w.NoDepartment.Token, HttpMethod.Get, "/api/RowSecurity/my-scope"));
            Assert.False(none.GetProperty("isUnrestricted").GetBoolean());
            Assert.Empty(none.GetProperty("supportingDepartmentIds").EnumerateArray());
            Assert.Contains("ستاد ندارید", none.GetProperty("reason").GetString());
        }

        [Fact]
        public void Every_protected_type_has_a_registered_policy_and_the_policy_names_are_unique()
        {
            using var scope = _factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            foreach (var rowType in new[] { typeof(EquipmentRow), typeof(FailureReportRow), typeof(TechnicianAssignmentRow), typeof(ApplicationUser), typeof(UserDirectoryRow), typeof(TechnicianInfo) })
                Assert.NotEmpty(services.GetServices(typeof(IRowPolicy<>).MakeGenericType(rowType)));

            var descriptors = services.GetServices<IRowPolicyDescriptor>().ToList();
            Assert.Equal(descriptors.Count, descriptors.Select(d => d.Name).Distinct().Count());
            var all = typeof(IRowPolicy<>).Assembly.GetTypes().Concat(typeof(UserRowPolicy).Assembly.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRowPolicy<>)))
                .Distinct().ToList();
            Assert.All(all, t => Assert.Contains(descriptors, d => d.GetType() == t));
        }

        // ------------------------------------------------------------------------------------------ lists, Excel, dashboard

        [Fact]
        public async Task The_lists_the_excel_files_and_the_dashboard_show_a_user_only_his_departments_data()
        {
            var w = await NewWorldAsync();

            // equipment of department A: the one the department holds, and the one held by a branch under it
            Assert.Equal(new[] { w.EquipmentOrgA, w.EquipmentA }, await IdsAsync(w.UserA.Token, "/api/Equipment"));
            Assert.Equal(new[] { w.EquipmentB }, await IdsAsync(w.UserB.Token, "/api/Equipment"));
            Assert.Empty(await IdsAsync(w.NoDepartment.Token, "/api/Equipment"));                      // no department: nothing but assigned work

            Assert.Equal(new[] { w.ReportA }, await IdsAsync(w.UserA.Token, "/api/FailureReport"));
            Assert.Equal(new[] { w.ReportB }, await IdsAsync(w.UserB.Token, "/api/FailureReport"));
            Assert.Empty(await IdsAsync(w.NoDepartment.Token, "/api/FailureReport"));

            // the branch without a department belongs to nobody except those who see everything
            foreach (var token in new[] { await AdminAsync(), w.Central.Token })
            {
                var equipment = await IdsAsync(token, "/api/Equipment?pageSize=100");
                foreach (var id in new[] { w.EquipmentA, w.EquipmentOrgA, w.EquipmentB, w.EquipmentNone })
                    Assert.Contains(id, equipment);
                var reports = await IdsAsync(token, "/api/FailureReport");
                foreach (var id in new[] { w.ReportA, w.ReportB, w.ReportNone })
                    Assert.Contains(id, reports);
            }
            Assert.DoesNotContain(w.EquipmentNone, await IdsAsync(w.UserA.Token, "/api/Equipment"));
            Assert.DoesNotContain(w.ReportNone, await IdsAsync(w.UserA.Token, "/api/FailureReport"));

            // Excel: the same rows as the list
            async Task<int> RowsAsync(string token, string url)
            {
                var response = await As(token, HttpMethod.Get, url);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var book = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
                return book.Worksheet(1).LastRowUsed()!.RowNumber() - 1;
            }
            Assert.Equal(2, await RowsAsync(w.UserA.Token, "/api/Equipment/export"));
            Assert.Equal(1, await RowsAsync(w.UserA.Token, "/api/FailureReport/export"));
            Assert.Equal(0, await RowsAsync(w.NoDepartment.Token, "/api/Equipment/export"));

            // dashboard: counts what the list lets him see
            var equipmentCard = await ReadDataAsync(await As(w.UserA.Token, HttpMethod.Get, "/api/Dashboard/equipment"));
            Assert.Equal(2, equipmentCard.GetProperty("total").GetInt32());
            Assert.Equal(2, equipmentCard.GetProperty("needsActionCount").GetInt32());
            Assert.Equal(1, (await ReadDataAsync(await As(w.UserA.Token, HttpMethod.Get, "/api/Dashboard/failure-reports"))).GetProperty("total").GetInt32());
            Assert.Equal(0, (await ReadDataAsync(await As(w.NoDepartment.Token, HttpMethod.Get, "/api/Dashboard/equipment"))).GetProperty("total").GetInt32());
            Assert.Equal(await TotalAsync(w.UserA.Token, "/api/Equipment"), equipmentCard.GetProperty("total").GetInt32());
        }

        // ------------------------------------------------------------------------------------------ by id, writes

        [Fact]
        public async Task A_record_of_another_department_answers_like_a_missing_one_for_every_read_by_id_and_is_logged()
        {
            var w = await NewWorldAsync();
            const long missing = 987654321;

            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}", $"/api/Equipment/{missing}");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}/location-history", $"/api/Equipment/{missing}/location-history");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}/technician", $"/api/Equipment/{missing}/technician");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}", $"/api/FailureReport/{missing}");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}/assignments", $"/api/FailureReport/{missing}/assignments");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportNone}", $"/api/FailureReport/{missing}");

            // his own are fine
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentA}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentOrgA}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportA}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentA}/location-history")).StatusCode);
            // and everything is open to the one who sees everything
            Assert.Equal(HttpStatusCode.OK, (await As(w.Central.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(await AdminAsync(), HttpMethod.Get, $"/api/FailureReport/{w.ReportNone}")).StatusCode);

            // the refusal of a hidden record is in the security log, the one of a missing record is not
            var entries = Sink.Snapshot().Select(Rendered).ToList();
            Assert.Contains(entries, e => e.Contains("Row access denied") && System.Text.RegularExpressions.Regex.IsMatch(e, $"Equipment\"? {w.EquipmentB}\\b"));
            Assert.DoesNotContain(entries, e => e.Contains("Row access denied") && System.Text.RegularExpressions.Regex.IsMatch(e, $"Equipment\"? {missing}\\b"));
        }

        [Fact]
        public async Task A_user_cannot_change_delete_or_move_what_he_cannot_see_and_nothing_changes()
        {
            var w = await NewWorldAsync();
            var admin = await AdminAsync();
            var before = await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}"));
            var reportBefore = await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}"));
            const long missing = 987654321;

            var update = new
            {
                owner = 1, yaasCode = "CHANGED", equipmentTypeId = w.TypeId, equipmentStatusId = (long)EquipmentStatusCode.UnderMaintenance, isActive = true,
            };
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Put, $"/api/Equipment/{w.EquipmentB}", $"/api/Equipment/{missing}", update);
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Delete, $"/api/Equipment/{w.EquipmentB}", $"/api/Equipment/{missing}");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Post, $"/api/Equipment/{w.EquipmentB}/move", $"/api/Equipment/{missing}/move", new { currentSupportingDepartmentId = w.DepartmentA });
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Delete, $"/api/FailureReport/{w.ReportB}", $"/api/FailureReport/{missing}");

            var after = await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}"));
            Assert.Equal(before.GetProperty("yaasCode").GetString(), after.GetProperty("yaasCode").GetString());
            Assert.Equal(before.GetProperty("rowVersion").GetString(), after.GetProperty("rowVersion").GetString());
            Assert.True(after.GetProperty("isActive").GetBoolean());
            Assert.True((await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}"))).GetProperty("isActive").GetBoolean());
            Assert.Equal(reportBefore.GetProperty("rowVersion").GetString(), (await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}"))).GetProperty("rowVersion").GetString());

            // bulk move: a hidden equipment is "not found" in the list of problems and nothing at all is moved
            var bulk = await As(w.UserA.Token, HttpMethod.Post, "/api/Equipment/bulk-move", new { equipmentIds = new[] { w.EquipmentA, w.EquipmentB }, currentSupportingDepartmentId = w.DepartmentA });
            Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
            var json = await ReadJsonAsync(bulk);
            Assert.Equal("bulk_items_invalid", ErrorCodeOf(json));
            Assert.Equal(new[] { "equipmentIds[1]" }, ErrorFieldsOf(json));
            Assert.Equal(w.DepartmentB, (await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}"))).GetProperty("currentSupportingDepartmentId").GetInt64());

            // his own can be changed and moved within his reach
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Post, $"/api/Equipment/{w.EquipmentA}/move", new { currentOrganizationId = w.OrganizationA })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Delete, $"/api/FailureReport/{w.ReportA}")).StatusCode);
            // the one who sees everything can change anything
            Assert.Equal(HttpStatusCode.OK, (await As(w.Central.Token, HttpMethod.Post, $"/api/Equipment/{w.EquipmentB}/move", new { currentOrganizationId = w.OrganizationB })).StatusCode);
        }

        [Fact]
        public async Task A_destination_must_be_one_the_user_can_see_for_create_move_bulk_create_and_a_new_report()
        {
            var w = await NewWorldAsync();

            await EquipmentAsync(w.UserA.Token, w, new { CurrentSupportingDepartmentId = w.DepartmentA });                                       // his own department
            await EquipmentAsync(w.UserA.Token, w, new { CurrentOrganizationId = w.OrganizationA });                                              // a branch under it
            await EquipmentAsync(w.UserA.Token, w, new { CurrentSupportingDepartmentId = w.DepartmentB }, HttpStatusCode.NotFound);               // another department
            await EquipmentAsync(w.UserA.Token, w, new { CurrentOrganizationId = w.OrganizationB }, HttpStatusCode.NotFound);                      // a branch under another
            await EquipmentAsync(w.UserA.Token, w, new { CurrentOrganizationId = w.OrganizationNone }, HttpStatusCode.NotFound);                   // a branch nobody serves
            await EquipmentAsync(w.Central.Token, w, new { CurrentSupportingDepartmentId = w.DepartmentB });                                      // the one who sees everything

            // moving out of his reach is refused, moving within it is fine
            var own = await EquipmentAsync(w.UserA.Token, w, new { CurrentSupportingDepartmentId = w.DepartmentA });
            var refused = await As(w.UserA.Token, HttpMethod.Post, $"/api/Equipment/{own}/move", new { currentSupportingDepartmentId = w.DepartmentB });
            Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(refused)));
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Post, $"/api/Equipment/{own}/move", new { currentOrganizationId = w.OrganizationA })).StatusCode);

            // bulk create: a row at another department is a problem of that row, and nothing is saved
            var bulk = await As(w.UserA.Token, HttpMethod.Post, "/api/Equipment/bulk", new
            {
                items = new object[]
                {
                    new { yaasCode = Uniq("RSB"), owner = 2, equipmentTypeId = w.TypeId, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentSupportingDepartmentId = w.DepartmentA },
                    new { yaasCode = Uniq("RSB"), owner = 2, equipmentTypeId = w.TypeId, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentSupportingDepartmentId = w.DepartmentB },
                },
            });
            Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
            Assert.Equal(new[] { "items[1].currentSupportingDepartmentId" }, ErrorFieldsOf(await ReadJsonAsync(bulk)));

            // a report: only for a branch he sees and for equipment he sees
            await ReportAsync(w.UserA.Token, w, w.OrganizationA, w.EquipmentA);
            await ReportAsync(w.UserA.Token, w, w.OrganizationB, w.EquipmentA, HttpStatusCode.NotFound);
            await ReportAsync(w.UserA.Token, w, w.OrganizationA, w.EquipmentB, HttpStatusCode.NotFound);
            await ReportAsync(w.UserA.Token, w, w.OrganizationNone, w.EquipmentA, HttpStatusCode.NotFound);
            await ReportAsync(w.Central.Token, w, w.OrganizationB, w.EquipmentB);
        }

        // ------------------------------------------------------------------------------------------ assignments

        [Fact]
        public async Task A_technician_sees_only_his_assigned_work_and_the_report_and_equipment_of_it_even_without_a_department()
        {
            var w = await NewWorldAsync();
            var admin = await AdminAsync();

            // before: he sees nothing of department B
            await AssertLooksMissingAsync(w.NoDepartment.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}", "/api/FailureReport/987654321");

            var assigned = await As(admin, HttpMethod.Post, $"/api/FailureReport/{w.ReportB}/assignments", new { technicianUserId = w.NoDepartment.Id, title = "تعمیر", priorityId = 3 });
            Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);
            var assignmentId = Id(await ReadDataAsync(assigned));

            // after: the assignment, its report and its equipment — and nothing else
            Assert.Equal(new[] { assignmentId }, await IdsAsync(w.NoDepartment.Token, "/api/TechnicianAssignment"));
            Assert.Equal(new[] { w.ReportB }, await IdsAsync(w.NoDepartment.Token, "/api/FailureReport"));
            Assert.Equal(new[] { w.EquipmentB }, await IdsAsync(w.NoDepartment.Token, "/api/Equipment"));
            Assert.Equal(HttpStatusCode.OK, (await As(w.NoDepartment.Token, HttpMethod.Get, $"/api/TechnicianAssignment/{assignmentId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.NoDepartment.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportB}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.NoDepartment.Token, HttpMethod.Get, $"/api/Equipment/{w.EquipmentB}")).StatusCode);
            await AssertLooksMissingAsync(w.NoDepartment.Token, HttpMethod.Get, $"/api/FailureReport/{w.ReportA}", "/api/FailureReport/987654321");
            Assert.Equal(1, (await ReadDataAsync(await As(w.NoDepartment.Token, HttpMethod.Get, "/api/Dashboard/assignments"))).GetProperty("total").GetInt32());

            // he works on it
            Assert.Equal(HttpStatusCode.OK, (await As(w.NoDepartment.Token, HttpMethod.Post, $"/api/TechnicianAssignment/{assignmentId}/accept", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.NoDepartment.Token, HttpMethod.Post, $"/api/TechnicianAssignment/{assignmentId}/start", new { })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.NoDepartment.Token, HttpMethod.Post, $"/api/TechnicianAssignment/{assignmentId}/complete", new { })).StatusCode);

            // and the work stays visible after it is done (the history of his own work)
            Assert.Equal(new[] { assignmentId }, await IdsAsync(w.NoDepartment.Token, "/api/TechnicianAssignment"));
            Assert.Equal(new[] { w.ReportB }, await IdsAsync(w.NoDepartment.Token, "/api/FailureReport"));

            // another technician of department A does not see it, but the department of the report does (through its report)
            Assert.DoesNotContain(assignmentId, await IdsAsync(w.UserA.Token, "/api/TechnicianAssignment"));
            Assert.Contains(assignmentId, await IdsAsync(w.UserB.Token, "/api/TechnicianAssignment"));
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/TechnicianAssignment/{assignmentId}", "/api/TechnicianAssignment/987654321");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/TechnicianAssignment/{assignmentId}/actions", "/api/TechnicianAssignment/987654321/actions");
        }

        [Fact]
        public async Task An_assignment_of_another_department_cannot_be_edited_and_looks_missing_while_the_own_one_can()
        {
            var w = await NewWorldAsync();
            var admin = await AdminAsync();
            var technicianB = await NewPersonAsync(w.DepartmentB, "TechnicianAssignments.Work");

            var assigned = await As(admin, HttpMethod.Post, $"/api/FailureReport/{w.ReportB}/assignments", new { technicianUserId = technicianB.Id, title = "تعمیر", priorityId = 3 });
            Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);
            var assignmentId = Id(await ReadDataAsync(assigned));
            var edit = new { title = "عنوان تازه", priorityId = 2 };

            // department A: the edit is refused exactly like an edit of a missing assignment, and nothing changes
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Put, $"/api/TechnicianAssignment/{assignmentId}", "/api/TechnicianAssignment/987654321", edit);
            var after = await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/TechnicianAssignment/{assignmentId}"));
            Assert.Equal("تعمیر", after.GetProperty("title").GetString());

            // department B (the department of the report) edits it
            var edited = await As(w.UserB.Token, HttpMethod.Put, $"/api/TechnicianAssignment/{assignmentId}", edit);
            Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
            Assert.Equal("عنوان تازه", (await ReadDataAsync(edited)).GetProperty("title").GetString());
        }

        [Fact]
        public async Task An_assignment_can_only_be_made_for_a_report_and_a_technician_the_user_sees_and_hidden_ones_look_missing()
        {
            var w = await NewWorldAsync();
            var technicianA = await NewPersonAsync(w.DepartmentA, "TechnicianAssignments.Work");
            var technicianB = await NewPersonAsync(w.DepartmentB, "TechnicianAssignments.Work");
            string Url(long report) => $"/api/FailureReport/{report}/assignments";
            object Body(long technician) => new { technicianUserId = technician, title = "تعمیر", priorityId = 2 };

            // his own report and technician: fine
            var ok = await As(w.UserA.Token, HttpMethod.Post, Url(w.ReportA), Body(technicianA.Id));
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            // a report of another department: not found; a technician of another department: not found
            Assert.Equal(HttpStatusCode.NotFound, (await As(w.UserA.Token, HttpMethod.Post, Url(w.ReportB), Body(technicianA.Id))).StatusCode);
            var hiddenTechnician = await As(w.UserA.Token, HttpMethod.Post, Url(w.ReportA), Body(technicianB.Id));
            Assert.Equal(HttpStatusCode.NotFound, hiddenTechnician.StatusCode);
            Assert.Equal("user_not_found", ErrorCodeOf(await ReadJsonAsync(hiddenTechnician)));
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Users/{technicianB.Id}/assignments", "/api/Users/987654321/assignments");

            // bulk: the hidden technician is a problem of its item and nothing is saved
            var report2 = await ReportAsync(w.UserA.Token, w, w.OrganizationA, w.EquipmentA);
            var bulk = await As(w.UserA.Token, HttpMethod.Post, $"{Url(report2)}/bulk", new { items = new object[] { Body(technicianA.Id), Body(technicianB.Id) } });
            Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
            Assert.Equal(new[] { "items[1].technicianUserId" }, ErrorFieldsOf(await ReadJsonAsync(bulk)));
            Assert.Empty((await ReadDataAsync(await As(await AdminAsync(), HttpMethod.Get, Url(report2)))).EnumerateArray());

            // the one who sees everything may choose any technician
            Assert.Equal(HttpStatusCode.Created, (await As(w.Central.Token, HttpMethod.Post, Url(w.ReportB), Body(technicianA.Id))).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ users

        [Fact]
        public async Task Users_and_the_technician_choices_are_limited_to_the_department_and_a_hidden_user_looks_missing()
        {
            var w = await NewWorldAsync();
            var colleague = await NewPersonAsync(w.DepartmentA, "Equipments.View");
            var outsider = await NewPersonAsync(w.DepartmentB, "Equipments.View");

            var users = await IdsAsync(w.UserA.Token, "/api/Users");
            Assert.Contains(colleague.Id, users);
            Assert.Contains(w.UserA.Id, users);
            Assert.DoesNotContain(outsider.Id, users);
            Assert.DoesNotContain(w.UserB.Id, users);
            Assert.Equal(new[] { w.NoDepartment.Id }, await IdsAsync(w.NoDepartment.Token, "/api/Users"));      // without a department: only himself

            // export = list
            var export = await As(w.UserA.Token, HttpMethod.Get, "/api/Users/export");
            using (var book = new XLWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync())))
                Assert.Equal(users.Count, book.Worksheet(1).LastRowUsed()!.RowNumber() - 1);

            // by id: the colleague yes, the outsider looks missing; and the outsider cannot be edited, deleted or given a role
            Assert.Equal(HttpStatusCode.OK, (await As(w.UserA.Token, HttpMethod.Get, $"/api/Users/{colleague.Id}")).StatusCode);
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Users/{outsider.Id}", "/api/Users/987654321");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Delete, $"/api/Users/{outsider.Id}", "/api/Users/987654321");
            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Users/{outsider.Id}/roles", "/api/Users/987654321/roles");
            Assert.True((await ReadDataAsync(await As(await AdminAsync(), HttpMethod.Get, $"/api/Users/{outsider.Id}"))).GetProperty("isActive").GetBoolean());

            // creating a user or moving one is only to a department he sees
            object NewUser(long? department) => new { userName = "n" + Guid.NewGuid().ToString("N")[..12], password = "Passw0rd!x", firstName = "x", lastName = "y", personnelCode = Uniq("PC"), supportingDepartmentIds = department.HasValue ? new[] { department.Value } : Array.Empty<long>() };
            var created = await As(w.UserA.Token, HttpMethod.Post, "/api/Users", NewUser(w.DepartmentA));
            Assert.True(created.StatusCode == HttpStatusCode.Created, await created.Content.ReadAsStringAsync());
            var wrong = await As(w.UserA.Token, HttpMethod.Post, "/api/Users", NewUser(w.DepartmentB));
            Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
            Assert.Equal("department_not_found", ErrorCodeOf(await ReadJsonAsync(wrong)));

            // the technician dropdown offers the same people
            var technicians = await IdsAsync(w.UserA.Token, "/api/Equipment/technicians");
            Assert.Contains(colleague.Id, technicians);
            Assert.DoesNotContain(outsider.Id, technicians);

            // the one who sees everything sees everyone
            // (looked up by what is unique to him: the shared test database holds more users than one page)
            foreach (var person in new[] { outsider, colleague })
            {
                Assert.Contains(person.Id, await IdsAsync(w.Central.Token, $"/api/Users?search={person.Code}"));
                Assert.Contains(person.Id, await IdsAsync(w.Central.Token, $"/api/Equipment/technicians?search={person.Code}"));
            }
        }

        // ------------------------------------------------------------------------------------------ attachments

        [Fact]
        public async Task The_files_of_an_equipment_are_visible_exactly_when_the_equipment_is()
        {
            var w = await NewWorldAsync();
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.Concat(Enumerable.Range(0, 500).Select(i => (byte)(i % 251))).ToArray();

            async Task<HttpResponseMessage> UploadAsync(string token, long equipmentId)
            {
                using var form = new MultipartFormDataContent();
                var file = new ByteArrayContent(png);
                file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(file, "file", "plan.png");
                var request = new HttpRequestMessage(HttpMethod.Post, $"/api/Attachments/Equipments/{equipmentId}") { Content = form };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await _client.SendAsync(request);
            }

            var attachmentB = Id(await ReadDataAsync(await UploadAsync(await AdminAsync(), w.EquipmentB)));
            Assert.Equal(HttpStatusCode.Created, (await UploadAsync(w.UserA.Token, w.EquipmentA)).StatusCode);                  // his own equipment

            var hiddenUpload = await UploadAsync(w.UserA.Token, w.EquipmentB);
            Assert.Equal(HttpStatusCode.NotFound, hiddenUpload.StatusCode);
            var missingUpload = await UploadAsync(w.UserA.Token, 987654321);
            Assert.Equal(await ReadJsonAsync(missingUpload).ContinueWith(t => ErrorCodeOf(t.Result)), ErrorCodeOf(await ReadJsonAsync(hiddenUpload)));

            await AssertLooksMissingAsync(w.UserA.Token, HttpMethod.Get, $"/api/Attachments/Equipments/{w.EquipmentB}", "/api/Attachments/Equipments/987654321");
            Assert.Equal(HttpStatusCode.NotFound, (await As(w.UserA.Token, HttpMethod.Get, $"/api/Attachments/{attachmentB}/download")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await As(w.UserA.Token, HttpMethod.Delete, $"/api/Attachments/{attachmentB}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(w.Central.Token, HttpMethod.Get, $"/api/Attachments/{attachmentB}/download")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await As(await AdminAsync(), HttpMethod.Get, $"/api/Attachments/Equipments/{w.EquipmentB}")).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ a user with several departments

        private async Task<JsonElement> UserAsync(string token, long id) => await ReadDataAsync(await As(token, HttpMethod.Get, $"/api/Users/{id}"));

        private static long[] DepartmentIdsOf(JsonElement user) => user.GetProperty("supportingDepartments").EnumerateArray().Select(Id).ToArray();

        private async Task<HttpResponseMessage> PutDepartmentsAsync(string token, long userId, params long[] departments)
        {
            var user = await UserAsync(await AdminAsync(), userId);
            return await As(token, HttpMethod.Put, $"/api/Users/{userId}", new
            {
                userName = user.GetProperty("userName").GetString(), firstName = user.GetProperty("firstName").GetString(), lastName = user.GetProperty("lastName").GetString(),
                personnelCode = user.GetProperty("personnelCode").GetString(), isActive = true, supportingDepartmentIds = departments,
            });
        }

        [Fact]
        public async Task A_user_with_two_departments_sees_the_data_of_both_and_nothing_of_a_third_and_is_seen_from_both()
        {
            var w = await NewWorldAsync();
            var admin = await AdminAsync();
            var city = await CatalogFixtures.EnsureCityAsync(this);
            var departmentC = await PostIdAsync("/api/SupportingDepartment", new { cityId = city, title = Uniq("rsC") });
            var organizationC = await NewOrganizationAsync(departmentC);
            var equipmentC = await EquipmentAsync(admin, w, new { CurrentSupportingDepartmentId = departmentC });
            var reportC = await ReportAsync(admin, w, organizationC, equipmentC);
            var userC = await NewPersonAsync(departmentC, FieldPermissions);

            var both = await NewPersonInAsync(new[] { w.DepartmentA, w.DepartmentB }, FieldPermissions);

            // the data of both departments, never of the third
            var reports = await IdsAsync(both.Token, "/api/FailureReport");
            Assert.Contains(w.ReportA, reports);
            Assert.Contains(w.ReportB, reports);
            Assert.DoesNotContain(reportC, reports);
            var equipment = await IdsAsync(both.Token, "/api/Equipment");
            Assert.Contains(w.EquipmentA, equipment);
            Assert.Contains(w.EquipmentOrgA, equipment);
            Assert.Contains(w.EquipmentB, equipment);
            Assert.DoesNotContain(equipmentC, equipment);
            await AssertLooksMissingAsync(both.Token, HttpMethod.Get, $"/api/FailureReport/{reportC}", "/api/FailureReport/987654321");

            // my-scope lists both departments
            var scope = await ReadDataAsync(await As(both.Token, HttpMethod.Get, "/api/RowSecurity/my-scope"));
            Assert.False(scope.GetProperty("isUnrestricted").GetBoolean());
            Assert.Equal(new[] { w.DepartmentA, w.DepartmentB }.OrderBy(x => x), scope.GetProperty("supportingDepartmentIds").EnumerateArray().Select(x => x.GetInt64()).OrderBy(x => x));
            Assert.Contains("ستادهای شما", scope.GetProperty("reason").GetString());
            var me = await ReadDataAsync(await As(both.Token, HttpMethod.Get, "/api/Identity/me"));
            Assert.Equal(2, me.GetProperty("supportingDepartments").GetArrayLength());

            // he is seen from each of his departments (users list and technician dropdown), not from another one
            foreach (var viewer in new[] { w.UserA, w.UserB })
            {
                Assert.Contains(both.Id, await IdsAsync(viewer.Token, "/api/Users"));
                Assert.Contains(both.Id, await IdsAsync(viewer.Token, "/api/Equipment/technicians"));
            }
            Assert.DoesNotContain(both.Id, await IdsAsync(userC.Token, "/api/Users"));
            Assert.DoesNotContain(both.Id, await IdsAsync(userC.Token, "/api/Equipment/technicians"));
            await AssertLooksMissingAsync(userC.Token, HttpMethod.Get, $"/api/Users/{both.Id}", "/api/Users/987654321");

            // wherever a person is named, ALL of his departments come with him
            var assigned = await As(admin, HttpMethod.Post, $"/api/FailureReport/{w.ReportA}/assignments", new { technicianUserId = both.Id, title = "تعمیر", priorityId = 3 });
            Assert.Equal(HttpStatusCode.Created, assigned.StatusCode);
            var assignment = await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/TechnicianAssignment/{Id(await ReadDataAsync(assigned))}"));
            Assert.Equal(2, assignment.GetProperty("technicianSupportingDepartments").GetArrayLength());
        }

        [Fact]
        public async Task The_departments_of_a_user_are_set_listed_filtered_replaced_and_checked()
        {
            var w = await NewWorldAsync();
            var admin = await AdminAsync();
            var name = "md_" + Guid.NewGuid().ToString("N")[..10];

            // a repeated id counts once; the list is ordered by title
            var created = await As(admin, HttpMethod.Post, "/api/Users", new
            {
                userName = name, password = "Passw0rd!x", firstName = "علی", lastName = "رضایی", personnelCode = "PC-" + name,
                supportingDepartmentIds = new[] { w.DepartmentB, w.DepartmentA, w.DepartmentB },
            });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = Id(await ReadDataAsync(created));
            Assert.Equal(new[] { w.DepartmentA, w.DepartmentB }, DepartmentIdsOf(await UserAsync(admin, id)));
            var listed = (await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/Users?supportingDepartmentId={w.DepartmentB}&pageSize=100"))).EnumerateArray().Single(u => Id(u) == id);
            Assert.Equal(2, listed.GetProperty("supportingDepartments").GetArrayLength());
            var first = listed.GetProperty("supportingDepartments")[0];
            foreach (var property in new[] { "id", "title", "cityId", "cityTitle", "provinceId", "provinceTitle" })
                Assert.True(first.TryGetProperty(property, out _), property);

            // the filter finds him by either department
            foreach (var department in new[] { w.DepartmentA, w.DepartmentB })
                Assert.Contains(id, (await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/Users?supportingDepartmentId={department}&pageSize=100"))).EnumerateArray().Select(Id));

            // an update replaces the whole list
            Assert.Equal(HttpStatusCode.OK, (await PutDepartmentsAsync(admin, id, w.DepartmentB)).StatusCode);
            Assert.Equal(new[] { w.DepartmentB }, DepartmentIdsOf(await UserAsync(admin, id)));
            Assert.DoesNotContain(id, (await ReadDataAsync(await As(admin, HttpMethod.Get, $"/api/Users?supportingDepartmentId={w.DepartmentA}&pageSize=100"))).EnumerateArray().Select(Id));
            Assert.Equal(HttpStatusCode.OK, (await PutDepartmentsAsync(admin, id)).StatusCode);
            Assert.Empty(DepartmentIdsOf(await UserAsync(admin, id)));

            // an unknown department and an invalid id are refused
            var unknown = await PutDepartmentsAsync(admin, id, w.DepartmentA, 987654321);
            Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
            Assert.Equal("department_not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));
            Assert.Equal(HttpStatusCode.BadRequest, (await PutDepartmentsAsync(admin, id, 0)).StatusCode);
            Assert.Empty(DepartmentIdsOf(await UserAsync(admin, id)));        // nothing was changed by the refused updates
        }

        [Fact]
        public async Task A_caller_can_only_add_or_remove_departments_he_sees_and_the_ones_he_does_not_see_stay()
        {
            var w = await NewWorldAsync();
            var city = await CatalogFixtures.EnsureCityAsync(this);
            var departmentC = await PostIdAsync("/api/SupportingDepartment", new { cityId = city, title = Uniq("rsC") });
            var editor = w.UserA;                                         // sees department A only
            var admin = await AdminAsync();
            var target = await NewPersonInAsync(new[] { w.DepartmentA, w.DepartmentB });

            // sending the list unchanged is fine (he may change the other fields)
            Assert.Equal(HttpStatusCode.OK, (await PutDepartmentsAsync(editor.Token, target.Id, w.DepartmentA, w.DepartmentB)).StatusCode);

            // taking out a department he cannot see, or putting the user into one he cannot see, is refused like a missing department
            foreach (var wanted in new[] { new[] { w.DepartmentA }, new[] { w.DepartmentA, w.DepartmentB, departmentC }, new[] { departmentC } })
            {
                var refused = await PutDepartmentsAsync(editor.Token, target.Id, wanted);
                Assert.Equal(HttpStatusCode.NotFound, refused.StatusCode);
                Assert.Equal("department_not_found", ErrorCodeOf(await ReadJsonAsync(refused)));
            }
            Assert.Equal(new[] { w.DepartmentA, w.DepartmentB }, DepartmentIdsOf(await UserAsync(admin, target.Id)).OrderBy(x => x));

            // he can add one he sees to a user he sees, and remove it again
            var onlyA = await NewPersonInAsync(new[] { w.DepartmentA });
            Assert.Equal(HttpStatusCode.OK, (await PutDepartmentsAsync(editor.Token, onlyA.Id, w.DepartmentA)).StatusCode);
        }
    }
}
