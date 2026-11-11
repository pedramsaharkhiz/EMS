using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>Technician assignments: assigning, the status flow, who may do what, actions, the report's status, filters and permissions.</summary>
    public class TechnicianAssignmentTests : ApiTestBase
    {
        public TechnicianAssignmentTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private Task<HttpResponseMessage> AsAdminAsync(HttpMethod method, string url, object? body = null) => AdminSendAsync(method, url, body);
        private async Task<HttpResponseMessage> AdminSendAsync(HttpMethod method, string url, object? body) => await SendAsync(method, url, body, bearer: await AdminAsync());
        private Task<long> PostIdAsync(string url, object body) => CatalogFixtures.PostAsync(this, url, body, "id");
        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        // ------------------------------------------------------------------------------------------ fixtures

        private sealed record Report(long Id, long EquipmentId);

        private async Task<Report> NewReportAsync()
        {
            var category = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("tacat") });
            var type = await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("tatype"), code = "T" });
            var department = await PostIdAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("taloc") });
            var equipment = await AsAdminAsync(HttpMethod.Post, "/api/Equipment", new
            {
                yaasCode = Uniq("TAY"), owner = 1, equipmentTypeId = type, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentSupportingDepartmentId = department
            });
            Assert.Equal(HttpStatusCode.Created, equipment.StatusCode);
            var equipmentId = Id(await ReadDataAsync(equipment));

            var failureCategory = await PostIdAsync("/api/FailureCategory", new { title = Uniq("tafc") });
            var failureType = await PostIdAsync("/api/FailureType", new { failureCategoryId = failureCategory, title = Uniq("taft"), code = "FT" });

            long organizationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                var zone = new Zone { Title = Uniq("tazone") };
                var management = new OrganizationManagement { Title = Uniq("tamgmt"), Zone = zone };
                var field = new Field { Title = Uniq("tafield"), OrganizationManagement = management };
                var organization = new Organization { Title = Uniq("taorg"), Field = field, OrganizationType = new OrganizationType { Title = Uniq("taotype"), CreatedByUserId = 1 } };
                db.Organizations.Add(organization);
                await db.SaveChangesAsync();
                organizationId = organization.OrganizationId;
            }

            var report = await AsAdminAsync(HttpMethod.Post, "/api/FailureReport", new
            {
                equipmentId, failureTypeId = failureType, reportTypeId = (long)ReportTypeCode.Failure, organizationId, priorityId = 3, description = "خراب است"
            });
            Assert.Equal(HttpStatusCode.Created, report.StatusCode);
            return new Report(Id(await ReadDataAsync(report)), equipmentId);
        }

        /// <summary>A user holding exactly these permissions, and his token.</summary>
        private async Task<(long UserId, string Token)> NewUserAsync(params string[] permissions)
        {
            var admin = await AdminAsync();
            var name = "ta_" + Guid.NewGuid().ToString("N")[..10];
            var userId = await CreateUserAsync(admin, name, "Passw0rd!x");
            var roleId = await CreateRoleAsync(admin, "role_" + name, permissions);
            await AssignRoleAsync(admin, userId, roleId);
            return (userId, (await LoginAsync(name, "Passw0rd!x")).AccessToken);
        }

        private Task<(long UserId, string Token)> NewTechnicianAsync() =>
            NewUserAsync("TechnicianAssignments.Work", "TechnicianAssignments.View");

        private async Task<JsonElement> AssignAsync(long reportId, long technicianId, object? extra = null)
        {
            var body = new Dictionary<string, object?> { ["technicianUserId"] = technicianId, ["description"] = "لطفاً بررسی شود" };
            if (extra is not null)
                foreach (var p in extra.GetType().GetProperties())
                    body[p.Name] = p.GetValue(extra);
            var response = await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{reportId}/assignments", body);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"assign -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadDataAsync(response);
        }

        private async Task<HttpResponseMessage> PostAsync(string token, long id, string verb, object? body = null) =>
            await SendAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{id}/{verb}", body ?? new { }, bearer: token);

        private async Task<string> ReportStatusCodeAsync(long reportId) =>
            (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{reportId}"))).GetProperty("failureReportStatusCode").GetString()!;

        private async Task<long> NewActionAsync() => await PostIdAsync("/api/Action", new { title = Uniq("taaction") });

        // ------------------------------------------------------------------------------------------ assign

        [Fact]
        public async Task Assign_creates_an_assigned_assignment_attributed_to_the_signed_in_user_and_shows_both_people()
        {
            var report = await NewReportAsync();
            var (technician, _) = await NewTechnicianAsync();
            var admin = await LoginAdminAsync();

            var a = await AssignAsync(report.Id, technician, new { deadlineDateTime = DateTime.UtcNow.AddDays(2) });

            Assert.Equal("Assigned", a.GetProperty("statusCode").GetString());
            Assert.Equal(report.Id, a.GetProperty("failureReportId").GetInt64());
            Assert.Equal(report.EquipmentId, a.GetProperty("equipmentId").GetInt64());
            Assert.Equal(technician, a.GetProperty("technicianUserId").GetInt64());
            Assert.Equal(admin.UserId, a.GetProperty("assignedByUserId").GetInt64());
            Assert.False(string.IsNullOrEmpty(a.GetProperty("technicianName").GetString()));
            Assert.False(string.IsNullOrEmpty(a.GetProperty("assignedByName").GetString()));
            Assert.False(a.GetProperty("isOverdue").GetBoolean());
            Assert.Equal("لطفاً بررسی شود", a.GetProperty("description").GetString());
            Assert.Equal(0, a.GetProperty("actions").GetArrayLength());

            var read = await AsAdminAsync(HttpMethod.Get, $"/api/TechnicianAssignment/{Id(a)}");
            Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        }

        [Fact]
        public async Task Assign_refuses_an_unknown_report_an_unknown_technician_a_past_deadline_and_a_closed_report()
        {
            var report = await NewReportAsync();
            var (technician, _) = await NewTechnicianAsync();

            var unknownReport = await AsAdminAsync(HttpMethod.Post, "/api/FailureReport/999999999/assignments", new { technicianUserId = technician });
            Assert.Equal(HttpStatusCode.NotFound, unknownReport.StatusCode);

            var unknownUser = await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{report.Id}/assignments", new { technicianUserId = 999999999 });
            Assert.Equal(HttpStatusCode.NotFound, unknownUser.StatusCode);
            Assert.Equal("user_not_found", ErrorCodeOf(await ReadJsonAsync(unknownUser)));

            var pastDeadline = await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{report.Id}/assignments", new { technicianUserId = technician, deadlineDateTime = DateTime.UtcNow.AddDays(-1) });
            Assert.Equal(HttpStatusCode.BadRequest, pastDeadline.StatusCode);
            Assert.Contains("deadlineDateTime", ErrorFieldsOf(await ReadJsonAsync(pastDeadline)), StringComparer.OrdinalIgnoreCase);

            await SetReportStatusAsync(report.Id, FailureReportStatusCode.Closed);
            var closed = await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{report.Id}/assignments", new { technicianUserId = technician });
            Assert.Equal(HttpStatusCode.Conflict, closed.StatusCode);
            Assert.Equal("failure_report_closed", ErrorCodeOf(await ReadJsonAsync(closed)));
        }

        [Fact]
        public async Task A_technician_has_one_open_assignment_per_report_and_can_be_assigned_again_after_a_cancel()
        {
            var report = await NewReportAsync();
            var (technician, _) = await NewTechnicianAsync();
            var first = await AssignAsync(report.Id, technician);

            var second = await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{report.Id}/assignments", new { technicianUserId = technician });
            Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
            Assert.Equal("assignment_already_open", ErrorCodeOf(await ReadJsonAsync(second)));

            var cancel = await AsAdminAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{Id(first)}/cancel", new { note = "تکنسین عوض شد" });
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
            var cancelled = await ReadDataAsync(cancel);
            Assert.Equal("Cancelled", cancelled.GetProperty("statusCode").GetString());
            Assert.Equal("تکنسین عوض شد", cancelled.GetProperty("resultNote").GetString());

            var again = await AssignAsync(report.Id, technician);
            Assert.NotEqual(Id(first), Id(again));

            // the report's history shows both, newest first when sorted so
            var history = await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{report.Id}/assignments?sortDirection=desc");
            var items = (await ReadDataAsync(history)).EnumerateArray().ToList();
            Assert.Equal(new[] { Id(again), Id(first) }, items.Select(Id));
        }

        [Fact]
        public async Task A_report_can_have_open_assignments_for_several_technicians_each_working_on_his_own()
        {
            var report = await NewReportAsync();
            var (electrician, electricianToken) = await NewTechnicianAsync();
            var (carpenter, carpenterToken) = await NewTechnicianAsync();

            var a = Id(await AssignAsync(report.Id, electrician));
            var b = Id(await AssignAsync(report.Id, carpenter));          // a second technician on the same report is fine
            Assert.NotEqual(a, b);

            // each one works on his own: one rejects, the other completes; neither touches the other's assignment
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(carpenterToken, b, "reject", new { note = "در مرخصی هستم" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(electricianToken, a, "accept")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(electricianToken, a, "start")).StatusCode);
            Assert.Equal("InProgress", await ReportStatusCodeAsync(report.Id));
            Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(electricianToken, b, "accept")).StatusCode);   // not his

            var history = DataOf(await ReadJsonAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{report.Id}/assignments?sortDirection=asc"))).EnumerateArray().ToList();
            Assert.Equal(new[] { "InProgress", "Rejected" }, history.Select(h => h.GetProperty("statusCode").GetString()));
        }

        // ------------------------------------------------------------------------------------------ bulk

        private async Task<HttpResponseMessage> BulkAsync(long reportId, params object[] items) =>
            await AsAdminAsync(HttpMethod.Post, $"/api/FailureReport/{reportId}/assignments/bulk", new { items });

        [Fact]
        public async Task Bulk_assigns_one_failure_to_several_technicians_each_with_his_own_task_and_deadline()
        {
            var report = await NewReportAsync();
            var (electrician, _) = await NewTechnicianAsync();
            var (carpenter, carpenterToken) = await NewTechnicianAsync();
            var admin = await LoginAdminAsync();
            var deadline = DateTime.UtcNow.AddDays(2);

            var response = await BulkAsync(report.Id,
                new { technicianUserId = electrician, description = "تعمیر برق دستگاه", deadlineDateTime = deadline },
                new { technicianUserId = carpenter, description = "تعمیر درب یخچال" });
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"bulk -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var created = (await ReadDataAsync(response)).EnumerateArray().ToList();

            Assert.Equal(2, created.Count);
            Assert.Equal(new[] { electrician, carpenter }, created.Select(c => c.GetProperty("technicianUserId").GetInt64()));          // the order of the request
            Assert.Equal(new[] { "تعمیر برق دستگاه", "تعمیر درب یخچال" }, created.Select(c => c.GetProperty("description").GetString()));
            Assert.All(created, c =>
            {
                Assert.Equal("Assigned", c.GetProperty("statusCode").GetString());
                Assert.Equal(report.Id, c.GetProperty("failureReportId").GetInt64());
                Assert.Equal(admin.UserId, c.GetProperty("assignedByUserId").GetInt64());
                Assert.False(string.IsNullOrEmpty(c.GetProperty("technicianName").GetString()));
            });
            Assert.NotEqual(JsonValueKind.Null, created[0].GetProperty("deadlineDateTime").ValueKind);
            Assert.Equal(JsonValueKind.Null, created[1].GetProperty("deadlineDateTime").ValueKind);

            // both are on the report's history and each technician sees only his own work on his own
            var history = DataOf(await ReadJsonAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{report.Id}/assignments"))).EnumerateArray().Select(Id).ToList();
            Assert.Equal(created.Select(Id).OrderBy(i => i), history.OrderBy(i => i));
            Assert.Equal(HttpStatusCode.OK, (await PostAsync(carpenterToken, Id(created[1]), "accept")).StatusCode);

            // the dashboard counts the assignments, and the report now has open work
            Assert.True(await HasOpenAssignmentAsync(report.Id));
        }

        [Fact]
        public async Task Bulk_is_all_or_nothing_and_lists_every_problem_item_by_item()
        {
            var report = await NewReportAsync();
            var (taken, _) = await NewTechnicianAsync();
            var (free, _) = await NewTechnicianAsync();
            await AssignAsync(report.Id, taken);                              // already has an open assignment on this report

            var response = await BulkAsync(report.Id,
                new { technicianUserId = free, description = "کار اول" },
                new { technicianUserId = taken, description = "کار دوم" },
                new { technicianUserId = 999999999L, description = "کار سوم" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("bulk_items_invalid", ErrorCodeOf(json));
            Assert.Equal(new[] { "items[1].technicianUserId", "items[2].technicianUserId" }, ErrorFieldsOf(json));

            // nothing was saved, not even the valid first item
            var all = DataOf(await ReadJsonAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{report.Id}/assignments"))).EnumerateArray().ToList();
            Assert.Single(all);
            Assert.Equal(taken, all[0].GetProperty("technicianUserId").GetInt64());
        }

        [Fact]
        public async Task Bulk_refuses_a_bad_request_before_touching_anything()
        {
            var report = await NewReportAsync();
            var (technician, _) = await NewTechnicianAsync();
            var (other, _) = await NewTechnicianAsync();

            async Task<(HttpStatusCode, List<string>)> ValidationAsync(params object[] items)
            {
                var response = await BulkAsync(report.Id, items);
                var json = await ReadJsonAsync(response);
                return (response.StatusCode, response.StatusCode == HttpStatusCode.BadRequest && json.GetProperty("error").TryGetProperty("details", out _) ? ErrorFieldsOf(json) : new List<string>());
            }

            Assert.Equal(HttpStatusCode.BadRequest, (await ValidationAsync()).Item1);                                                                 // empty list
            var noTask = await ValidationAsync(new { technicianUserId = technician });
            Assert.Equal(HttpStatusCode.BadRequest, noTask.Item1);
            Assert.Contains("items[0].description", noTask.Item2, StringComparer.OrdinalIgnoreCase);                                                      // the task is required in a bulk
            var past = await ValidationAsync(new { technicianUserId = technician, description = "x", deadlineDateTime = DateTime.UtcNow.AddDays(-1) });
            Assert.Contains("items[0].deadlineDateTime", past.Item2, StringComparer.OrdinalIgnoreCase);
            var twice = await ValidationAsync(new { technicianUserId = technician, description = "الف" }, new { technicianUserId = technician, description = "ب" });
            Assert.Contains("items[1].technicianUserId", twice.Item2, StringComparer.OrdinalIgnoreCase);                                                  // one technician once

            var tooMany = Enumerable.Range(0, 21).Select(_ => (object)new { technicianUserId = other, description = "x" }).ToArray();
            Assert.Equal(HttpStatusCode.BadRequest, (await BulkAsync(report.Id, tooMany)).StatusCode);                                               // at most 20

            Assert.Empty(DataOf(await ReadJsonAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{report.Id}/assignments"))).EnumerateArray());
        }

        [Fact]
        public async Task Bulk_needs_an_assignable_report_and_the_assign_permission()
        {
            var report = await NewReportAsync();
            var (technician, _) = await NewTechnicianAsync();
            var item = new { technicianUserId = technician, description = "تعمیر" };

            Assert.Equal(HttpStatusCode.NotFound, (await BulkAsync(999999999, item)).StatusCode);

            await SetReportStatusAsync(report.Id, FailureReportStatusCode.Closed);
            var closed = await BulkAsync(report.Id, item);
            Assert.Equal(HttpStatusCode.Conflict, closed.StatusCode);
            Assert.Equal("failure_report_closed", ErrorCodeOf(await ReadJsonAsync(closed)));

            var open = await NewReportAsync();
            var viewer = (await NewUserAsync("TechnicianAssignments.View")).Token;
            var worker = (await NewUserAsync("TechnicianAssignments.Work")).Token;
            var assigner = (await NewUserAsync("TechnicianAssignments.Assign")).Token;
            var url = $"/api/FailureReport/{open.Id}/assignments/bulk";
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, url, viewer, new { items = new[] { item } }));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, url, worker, new { items = new[] { item } }));
            Assert.Equal(HttpStatusCode.Created, await StatusOfAsync(HttpMethod.Post, url, assigner, new { items = new[] { item } }));
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Post, url, new { items = new[] { item } })).StatusCode);
        }

        [Fact]
        public async Task The_history_of_an_unknown_report_is_404()
        {
            Assert.Equal(HttpStatusCode.NotFound, (await AsAdminAsync(HttpMethod.Get, "/api/FailureReport/999999999/assignments")).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ the flow

        [Fact]
        public async Task The_technician_accepts_starts_records_actions_and_completes_and_the_report_follows_only_at_the_start()
        {
            var report = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));
            Assert.Equal("Open", await ReportStatusCodeAsync(report.Id));

            var accepted = await PostAsync(token, id, "accept");
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
            Assert.Equal("Accepted", (await ReadDataAsync(accepted)).GetProperty("statusCode").GetString());
            Assert.Equal("Open", await ReportStatusCodeAsync(report.Id));

            var started = await PostAsync(token, id, "start");
            Assert.Equal("InProgress", (await ReadDataAsync(started)).GetProperty("statusCode").GetString());
            Assert.Equal("InProgress", await ReportStatusCodeAsync(report.Id));

            var action = await NewActionAsync();
            var recorded = await PostAsync(token, id, "actions", new { actionId = action, description = "قطعه عوض شد" });
            Assert.Equal(HttpStatusCode.Created, recorded.StatusCode);
            var item = await ReadDataAsync(recorded);
            Assert.Equal(action, item.GetProperty("actionId").GetInt64());
            Assert.Equal(technician, item.GetProperty("recordedByUserId").GetInt64());
            Assert.Equal(JsonValueKind.Null, item.GetProperty("equipmentLocationHistoryId").ValueKind);

            var completed = await PostAsync(token, id, "complete", new { note = "تمام شد" });
            var done = await ReadDataAsync(completed);
            Assert.Equal("Completed", done.GetProperty("statusCode").GetString());
            Assert.NotEqual(JsonValueKind.Null, done.GetProperty("completedDateTime").ValueKind);
            Assert.Equal("تمام شد", done.GetProperty("resultNote").GetString());
            Assert.Equal(1, done.GetProperty("actionCount").GetInt32());
            Assert.Equal(1, done.GetProperty("actions").GetArrayLength());
            // completing does not close the report
            Assert.Equal("InProgress", await ReportStatusCodeAsync(report.Id));

            var actions = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/TechnicianAssignment/{id}/actions"));
            Assert.Equal(1, actions.GetArrayLength());

            // and a finished assignment lets the report be assigned again
            await AssignAsync(report.Id, technician);
        }

        [Fact]
        public async Task Only_the_assigned_technician_can_work_on_it_even_with_the_permission_or_as_the_administrator()
        {
            var report = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var (_, other) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));

            foreach (var who in new[] { other, await AdminAsync() })
            {
                var response = await PostAsync(who, id, "accept");
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Equal("forbidden", ErrorCodeOf(await ReadJsonAsync(response)));
            }

            Assert.Equal(HttpStatusCode.OK, (await PostAsync(token, id, "accept")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(other, id, "start")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await PostAsync(other, id, "actions", new { actionId = 1 })).StatusCode);
        }

        [Fact]
        public async Task The_technician_can_reject_with_a_reason_and_a_rejected_assignment_is_final()
        {
            var report = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));

            var noReason = await PostAsync(token, id, "reject", new { });
            Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);

            var rejected = await PostAsync(token, id, "reject", new { note = "در مرخصی هستم" });
            Assert.Equal("Rejected", (await ReadDataAsync(rejected)).GetProperty("statusCode").GetString());

            var after = await PostAsync(token, id, "accept");
            Assert.Equal(HttpStatusCode.Conflict, after.StatusCode);
            Assert.Equal("assignment_transition_not_allowed", ErrorCodeOf(await ReadJsonAsync(after)));
            Assert.Equal(HttpStatusCode.Conflict, (await AsAdminAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{id}/cancel", new { note = "x" })).StatusCode);
        }

        [Fact]
        public async Task Steps_cannot_be_skipped_and_cancelling_needs_a_reason()
        {
            var report = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));

            Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(token, id, "start")).StatusCode);       // not accepted yet
            Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(token, id, "complete")).StatusCode);

            Assert.Equal(HttpStatusCode.BadRequest, (await AsAdminAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{id}/cancel", new { })).StatusCode);

            await PostAsync(token, id, "accept");
            Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(token, id, "complete")).StatusCode);    // not started yet
            Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(token, id, "reject", new { note = "x" })).StatusCode);   // reject only from Assigned
            Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(token, id, "accept")).StatusCode);
        }

        [Fact]
        public async Task An_accepted_or_started_assignment_can_be_cancelled_by_the_assigner()
        {
            var report = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));
            await PostAsync(token, id, "accept");
            await PostAsync(token, id, "start");

            var cancel = await AsAdminAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{id}/cancel", new { note = "اشتباه ارجاع شد" });
            Assert.Equal("Cancelled", (await ReadDataAsync(cancel)).GetProperty("statusCode").GetString());
        }

        [Fact]
        public async Task A_stale_row_version_is_refused_with_409()
        {
            var report = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var created = await AssignAsync(report.Id, technician);
            var id = Id(created);
            var staleVersion = created.GetProperty("rowVersion").GetString();

            Assert.Equal(HttpStatusCode.OK, (await PostAsync(token, id, "accept")).StatusCode);

            var stale = await PostAsync(token, id, "start", new { rowVersion = staleVersion });
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("concurrency_conflict", ErrorCodeOf(await ReadJsonAsync(stale)));
        }

        // ------------------------------------------------------------------------------------------ actions

        [Fact]
        public async Task An_action_needs_an_assignment_in_progress_an_existing_action_and_a_history_row_of_the_same_equipment()
        {
            var report = await NewReportAsync();
            var other = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));
            var action = await NewActionAsync();

            var early = await PostAsync(token, id, "actions", new { actionId = action });
            Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
            Assert.Equal("assignment_not_in_progress", ErrorCodeOf(await ReadJsonAsync(early)));

            await PostAsync(token, id, "accept");
            await PostAsync(token, id, "start");

            var unknown = await PostAsync(token, id, "actions", new { actionId = 999999999 });
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));

            var future = await PostAsync(token, id, "actions", new { actionId = action, actionDateTime = DateTime.UtcNow.AddDays(1) });
            Assert.Equal(HttpStatusCode.BadRequest, future.StatusCode);

            var ownHistory = await HistoryIdOfAsync(report.EquipmentId);
            var othersHistory = await HistoryIdOfAsync(other.EquipmentId);

            var mismatch = await PostAsync(token, id, "actions", new { actionId = action, equipmentLocationHistoryId = othersHistory });
            Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await PostAsync(token, id, "actions", new { actionId = action, equipmentLocationHistoryId = 999999999 })).StatusCode);

            var ok = await PostAsync(token, id, "actions", new { actionId = action, equipmentLocationHistoryId = ownHistory });
            Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
            Assert.Equal(ownHistory, (await ReadDataAsync(ok)).GetProperty("equipmentLocationHistoryId").GetInt64());

            // after completion nothing more can be recorded
            await PostAsync(token, id, "complete");
            Assert.Equal(HttpStatusCode.Conflict, (await PostAsync(token, id, "actions", new { actionId = action })).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ lists

        [Fact]
        public async Task The_list_filters_by_technician_status_report_and_overdue_and_users_have_their_own_route()
        {
            var report1 = await NewReportAsync();
            var report2 = await NewReportAsync();
            var (technician, token) = await NewTechnicianAsync();
            var (other, _) = await NewTechnicianAsync();

            var a1 = Id(await AssignAsync(report1.Id, technician, new { deadlineDateTime = DateTime.UtcNow.AddDays(3) }));
            var a2 = Id(await AssignAsync(report2.Id, technician, new { deadlineDateTime = DateTime.UtcNow.AddDays(3) }));
            await PostAsync(token, a2, "accept");
            await PostAsync(token, a2, "start");

            async Task<List<long>> IdsAsync(string query)
            {
                var response = await AsAdminAsync(HttpMethod.Get, $"/api/TechnicianAssignment?pageSize=100&sortDirection=desc&{query}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return (await ReadDataAsync(response)).EnumerateArray().Select(Id).ToList();
            }

            Assert.Equal(new[] { a2, a1 }, await IdsAsync($"technicianUserId={technician}"));
            Assert.Empty(await IdsAsync($"technicianUserId={other}"));
            Assert.Equal(new[] { a2 }, await IdsAsync($"technicianUserId={technician}&statusId={(long)TechnicianAssignmentStatusCode.InProgress}"));
            Assert.Equal(new[] { a1 }, await IdsAsync($"failureReportId={report1.Id}"));
            Assert.Empty(await IdsAsync($"technicianUserId={technician}&overdue=true"));

            // make a1 overdue: its deadline passed while it was still open
            await SetDeadlineAsync(a1, DateTime.UtcNow.AddHours(-2));
            Assert.Equal(new[] { a1 }, await IdsAsync($"technicianUserId={technician}&overdue=true"));
            Assert.Equal(new[] { a2 }, await IdsAsync($"technicianUserId={technician}&overdue=false"));
            var overdue = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/TechnicianAssignment/{a1}"));
            Assert.True(overdue.GetProperty("isOverdue").GetBoolean());

            // a finished assignment is never overdue
            await PostAsync(token, a2, "complete");
            await SetDeadlineAsync(a2, DateTime.UtcNow.AddHours(-2));
            Assert.Equal(new[] { a1 }, await IdsAsync($"technicianUserId={technician}&overdue=true"));

            var mine = await AsAdminAsync(HttpMethod.Get, $"/api/Users/{technician}/assignments?sortDirection=desc");
            Assert.Equal(new[] { a2, a1 }, (await ReadDataAsync(mine)).EnumerateArray().Select(Id));
            Assert.Equal(HttpStatusCode.NotFound, (await AsAdminAsync(HttpMethod.Get, "/api/Users/999999999/assignments")).StatusCode);
        }

        [Fact]
        public async Task The_statuses_lookup_lists_the_six_system_statuses_and_marks_the_open_ones()
        {
            var user = await NewUserAsync();   // no permission at all: a lookup needs only a signed-in user
            var response = await SendAsync(HttpMethod.Get, "/api/TechnicianAssignment/statuses", bearer: user.Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var items = (await ReadDataAsync(response)).EnumerateArray().ToList();

            Assert.Equal(new[] { "Assigned", "Accepted", "Rejected", "InProgress", "Completed", "Cancelled" }, items.Select(i => i.GetProperty("code").GetString()));
            Assert.Equal(new[] { 1L, 2L, 3L, 4L, 5L, 6L }, items.Select(Id));
            Assert.Equal(new[] { true, true, false, true, false, false }, items.Select(i => i.GetProperty("isOpen").GetBoolean()));
        }

        // ------------------------------------------------------------------------------------------ permissions

        [Fact]
        public async Task Every_endpoint_needs_its_permission()
        {
            var report = await NewReportAsync();
            var (technician, _) = await NewTechnicianAsync();
            var id = Id(await AssignAsync(report.Id, technician));
            var nobody = (await NewUserAsync()).Token;
            var viewer = (await NewUserAsync("TechnicianAssignments.View")).Token;
            var assigner = (await NewUserAsync("TechnicianAssignments.Assign")).Token;

            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/TechnicianAssignment", nobody));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, $"/api/TechnicianAssignment/{id}", nobody));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, $"/api/FailureReport/{report.Id}/assignments", nobody));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, $"/api/Users/{technician}/assignments", nobody));
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/TechnicianAssignment", viewer));

            // assign / cancel: the Assign permission; viewing and working are not enough
            var body = new { technicianUserId = technician };
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, $"/api/FailureReport/{report.Id}/assignments", viewer, body));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{id}/cancel", viewer, new { note = "x" }));
            Assert.Equal(HttpStatusCode.Conflict, await StatusOfAsync(HttpMethod.Post, $"/api/FailureReport/{report.Id}/assignments", assigner, body));   // allowed, but one is already open

            // working: the Work permission
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, $"/api/TechnicianAssignment/{id}/accept", assigner, new { }));
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/TechnicianAssignment")).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ database helpers

        private async Task<bool> HasOpenAssignmentAsync(long reportId) =>
            (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/FailureReport/{reportId}"))).GetProperty("hasOpenAssignment").GetBoolean();

        private async Task SetReportStatusAsync(long reportId, FailureReportStatusCode status)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var report = await db.FailureReports.IgnoreQueryFilters().FirstAsync(x => x.FailureReportId == reportId);
            report.FailureReportStatusId = (long)status;
            await db.SaveChangesAsync();
        }

        private async Task SetDeadlineAsync(long assignmentId, DateTime deadline)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var assignment = await db.TechnicianAssignments.FirstAsync(x => x.TechnicianAssignmentId == assignmentId);
            assignment.DeadlineDateTime = deadline;
            await db.SaveChangesAsync();
        }

        private async Task<long> HistoryIdOfAsync(long equipmentId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            return await db.Set<EquipmentLocationHistory>().Where(x => x.EquipmentId == equipmentId).Select(x => x.EquipmentLocationHistoryId).FirstAsync();
        }
    }
}
