using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>Failure reports: create / read / update / soft delete / restore, references, reference number, filters and permissions.</summary>
    public class FailureReportTests : ApiTestBase
    {
        public FailureReportTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private static long Open => (long)FailureReportStatusCode.Open;
        private static long InProgress => (long)FailureReportStatusCode.InProgress;
        private static long Closed => (long)FailureReportStatusCode.Closed;
        private static long FailureReportType => (long)ReportTypeCode.Failure;

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private Task<long> PostIdAsync(string url, object body) => CatalogFixtures.PostAsync(this, url, body, "id");

        // ------------------------------------------------------------------------------------------ fixtures

        private sealed record Fixture(long EquipmentId, string YaasCode, long CategoryId, long FailureTypeId, long OrganizationId);

        private async Task<long> NewEquipmentAsync(string? yaas = null)
        {
            var category = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("frcat") });
            var type = await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("frtype"), code = "T" });
            var location = await PostIdAsync("/api/EquipmentLocationType", new { title = Uniq("frloc"), code = "L" });
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", new
            {
                yaasCode = yaas ?? Uniq("FRY"), owner = 1, equipmentTypeId = type, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentEquipmentLocationTypeId = location
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<(long CategoryId, long TypeId)> NewFailureTypeAsync()
        {
            var category = await PostIdAsync("/api/FailureCategory", new { title = Uniq("frfc") });
            var type = await PostIdAsync("/api/FailureType", new { failureCategoryId = category, title = Uniq("frft"), code = "FT" });
            return (category, type);
        }

        private async Task<long> NewOrganizationAsync(bool active = true)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var zone = new Zone { Title = Uniq("frzone") };
            var management = new OrganizationManagement { Title = Uniq("frmgmt"), Zone = zone };
            var field = new Field { Title = Uniq("frfield"), OrganizationManagement = management };
            var type = new OrganizationType { Title = Uniq("frotype"), CreatedByUserId = 1 };
            var organization = new Organization { Title = Uniq("frorg"), Field = field, OrganizationType = type, IsActive = active };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
            return organization.OrganizationId;
        }

        private async Task<long> NewEmployeeAsync(long organizationId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var structure = new OrganizationStructure { Title = Uniq("frstruct") };
            var employee = new Employee
            {
                PersonnelCode = Uniq("EP"), FirstName = "علی", LastName = "رضایی", NationalCode = Guid.NewGuid().ToString("N")[..10],
                BirthDate = new DateTime(1990, 1, 1), OrganizationId = organizationId, OrganizationStructure = structure
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee.EmployeeId;
        }

        private async Task<Fixture> NewFixtureAsync()
        {
            var equipment = await NewEquipmentAsync();
            var (category, type) = await NewFailureTypeAsync();
            return new Fixture(equipment, "", category, type, await NewOrganizationAsync());
        }

        private static Dictionary<string, object?> Body(Fixture f, string? reference = null, int priority = 3) => new()
        {
            ["equipmentId"] = f.EquipmentId, ["failureTypeId"] = f.FailureTypeId, ["reportTypeId"] = FailureReportType,
            ["organizationId"] = f.OrganizationId, ["referenceNumber"] = reference, ["priorityId"] = priority, ["description"] = "دستگاه روشن نمی‌شود",
        };

        private async Task<JsonElement> CreateAsync(Dictionary<string, object?> body)
        {
            var response = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", body);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"create -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadDataAsync(response);
        }

        private static Dictionary<string, object?> UpdateBodyFrom(JsonElement report) => new()
        {
            ["equipmentId"] = report.GetProperty("equipmentId").GetInt64(),
            ["failureTypeId"] = report.GetProperty("failureTypeId").GetInt64(),
            ["reportTypeId"] = report.GetProperty("reportTypeId").GetInt64(),
            ["failureReportStatusId"] = report.GetProperty("failureReportStatusId").GetInt64(),
            ["organizationId"] = report.GetProperty("organizationId").GetInt64(),
            ["employeeId"] = report.GetProperty("employeeId").ValueKind == JsonValueKind.Null ? null : report.GetProperty("employeeId").GetInt64(),
            ["referenceNumber"] = report.GetProperty("referenceNumber").GetString(),
            ["priorityId"] = report.GetProperty("priorityId").GetInt32(),
            ["description"] = report.GetProperty("description").GetString(),
            ["isActive"] = true,
            ["rowVersion"] = report.GetProperty("rowVersion").GetString(),
        };

        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        private async Task<List<long>> ListIdsAsync(string query)
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport?pageSize=100&{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return DataOf(await ReadJsonAsync(response)).EnumerateArray().Select(Id).ToList();
        }

        // ------------------------------------------------------------------------------------------ create & read

        [Fact]
        public async Task Create_starts_open_is_attributed_to_the_signed_in_user_and_can_be_read_back()
        {
            var f = await NewFixtureAsync();
            var admin = await LoginAdminAsync();
            var before = DateTime.UtcNow.AddMinutes(-1);
            var report = await CreateAsync(Body(f, Uniq("REF"), priority: 5));

            Assert.Equal(Open, report.GetProperty("failureReportStatusId").GetInt64());
            Assert.Equal("Open", report.GetProperty("failureReportStatusCode").GetString());
            Assert.Equal("باز", report.GetProperty("failureReportStatusTitle").GetString());
            Assert.Equal(admin.UserId, report.GetProperty("reportedByUserId").GetInt64());
            Assert.Equal(5, report.GetProperty("priorityId").GetInt32());
            Assert.Equal("فوری", report.GetProperty("priorityTitle").GetString());
            Assert.Equal(f.CategoryId, report.GetProperty("failureCategoryId").GetInt64());
            Assert.True(report.GetProperty("isActive").GetBoolean());
            Assert.False(string.IsNullOrEmpty(report.GetProperty("rowVersion").GetString()));
            Assert.True(report.GetProperty("reportedDateTime").GetDateTime() >= before);
            Assert.EndsWith("Z", report.GetProperty("reportedDateTime").GetString());

            var again = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport/{Id(report)}"));
            Assert.Equal(report.GetProperty("referenceNumber").GetString(), again.GetProperty("referenceNumber").GetString());
            Assert.Equal(f.EquipmentId, again.GetProperty("equipmentId").GetInt64());
            Assert.False(string.IsNullOrEmpty(again.GetProperty("equipmentYaasCode").GetString()));
            // the equipment is described with the same names as in the equipment list
            Assert.Equal(1, again.GetProperty("equipmentOwner").GetInt32());
            Assert.Equal("شرکت", again.GetProperty("equipmentOwnerTitle").GetString());
            Assert.Equal("InStock", again.GetProperty("equipmentStatusCode").GetString());
            Assert.False(string.IsNullOrEmpty(again.GetProperty("equipmentStatusTitle").GetString()));
            Assert.True(again.GetProperty("equipmentTypeId").GetInt64() > 0);
            Assert.False(string.IsNullOrEmpty(again.GetProperty("equipmentTypeTitle").GetString()));
            Assert.True(again.TryGetProperty("failureTypeCode", out _) && again.TryGetProperty("reportTypeCode", out _));
            Assert.False(string.IsNullOrEmpty(again.GetProperty("organizationTitle").GetString()));
            Assert.False(string.IsNullOrEmpty(again.GetProperty("reportedByName").GetString()));
        }

        [Fact]
        public async Task The_reported_time_is_kept_when_given_and_may_not_be_in_the_future()
        {
            var f = await NewFixtureAsync();
            var body = Body(f);
            body["reportedDateTime"] = "2026-01-02T03:04:05Z";
            var report = await CreateAsync(body);
            Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), report.GetProperty("reportedDateTime").GetDateTime().ToUniversalTime());

            var future = Body(f);
            future["reportedDateTime"] = DateTime.UtcNow.AddDays(2).ToString("O");
            var refused = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", future);
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Contains("reportedDateTime", ErrorFieldsOf(await ReadJsonAsync(refused)));
        }

        [Fact]
        public async Task Invalid_input_is_a_400_validation_error_with_fields()
        {
            var f = await NewFixtureAsync();
            var body = Body(f, new string('r', 51), priority: 6);
            body["equipmentId"] = 0; body["description"] = new string('d', 501); body["employeeId"] = 0;
            var response = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("validation_failed", ErrorCodeOf(json));
            var fields = ErrorFieldsOf(json);
            foreach (var field in new[] { "equipmentId", "referenceNumber", "priorityId", "description", "employeeId" })
                Assert.Contains(field, fields);
        }

        // ------------------------------------------------------------------------------------------ references

        [Fact]
        public async Task Unknown_or_inactive_references_are_refused_with_the_catalog_codes()
        {
            var f = await NewFixtureAsync();

            foreach (var key in new[] { "equipmentId", "failureTypeId", "reportTypeId", "organizationId", "employeeId" })
            {
                var body = Body(f); body[key] = 99999999;
                var response = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", body);
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"{key} -> {(int)response.StatusCode}");
                Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(response)));
            }

            // deactivated equipment / failure type / organization
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{f.EquipmentId}")).StatusCode);
            var inactiveEquipment = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", Body(f));
            Assert.Equal(HttpStatusCode.BadRequest, inactiveEquipment.StatusCode);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(inactiveEquipment)));

            var g = await NewFixtureAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureType/{g.FailureTypeId}")).StatusCode);
            var inactiveType = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", Body(g));
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(inactiveType)));

            var inactiveOrg = await NewOrganizationAsync(active: false);
            var h = await NewFixtureAsync();
            var orgBody = Body(h); orgBody["organizationId"] = inactiveOrg;
            var refused = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", orgBody);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(refused)));
        }

        [Fact]
        public async Task An_employee_can_be_named()
        {
            var f = await NewFixtureAsync();
            var employee = await NewEmployeeAsync(f.OrganizationId);
            var body = Body(f); body["employeeId"] = employee;
            var report = await CreateAsync(body);
            Assert.Equal(employee, report.GetProperty("employeeId").GetInt64());
            Assert.Equal("علی رضایی", report.GetProperty("employeeName").GetString());
        }

        // ------------------------------------------------------------------------------------------ reference number

        [Fact]
        public async Task The_reference_number_is_optional_and_unique_among_active_reports_ignoring_case()
        {
            var f = await NewFixtureAsync();
            Assert.Equal(JsonValueKind.Null, (await CreateAsync(Body(f))).GetProperty("referenceNumber").ValueKind);
            await CreateAsync(Body(f));   // two reports without a number are fine

            var number = Uniq("Ref");
            var first = await CreateAsync(Body(f, number));
            var duplicate = await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", Body(f, number.ToUpperInvariant()));
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("duplicate_reference_number", ErrorCodeOf(await ReadJsonAsync(duplicate)));

            // updating the report with its own number is fine; deleting it frees the number
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(first)}", UpdateBodyFrom(first))).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureReport/{Id(first)}")).StatusCode);
            var reused = await CreateAsync(Body(f, number));

            // restoring the old one onto the number now held by another report is a conflict
            var restore = UpdateBodyFrom(first); restore["rowVersion"] = null;
            var conflict = await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(first)}", restore);
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("duplicate_reference_number", ErrorCodeOf(await ReadJsonAsync(conflict)));
            Assert.NotEqual(Id(first), Id(reused));
        }

        // ------------------------------------------------------------------------------------------ update

        [Fact]
        public async Task Update_changes_the_status_and_fields_keeps_the_reporter_and_checks_the_row_version()
        {
            var f = await NewFixtureAsync();
            var report = await CreateAsync(Body(f, Uniq("U")));
            var reporter = report.GetProperty("reportedByUserId").GetInt64();
            var reportedAt = report.GetProperty("reportedDateTime").GetString();

            var body = UpdateBodyFrom(report);
            body["failureReportStatusId"] = InProgress; body["priorityId"] = 1; body["description"] = null;
            var updated = await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(report)}", body);
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            var json = await ReadDataAsync(updated);
            Assert.Equal("InProgress", json.GetProperty("failureReportStatusCode").GetString());
            Assert.Equal("کم", json.GetProperty("priorityTitle").GetString());
            Assert.Equal(JsonValueKind.Null, json.GetProperty("description").ValueKind);   // not sent = cleared
            Assert.Equal(reporter, json.GetProperty("reportedByUserId").GetInt64());
            Assert.Equal(reportedAt, json.GetProperty("reportedDateTime").GetString());     // not sent = kept

            // the old row version is stale now
            var stale = await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(report)}", body);
            Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
            Assert.Equal("concurrency_conflict", ErrorCodeOf(await ReadJsonAsync(stale)));

            var closeBody = UpdateBodyFrom(json); closeBody["failureReportStatusId"] = Closed;
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(report)}", closeBody)).StatusCode);

            var unknown = await SendAdminAsync(HttpMethod.Put, "/api/FailureReport/99999999", UpdateBodyFrom(report));
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        }

        [Fact]
        public async Task A_report_keeps_working_after_its_equipment_was_deactivated_and_deactivate_and_restore_work()
        {
            var f = await NewFixtureAsync();
            var report = await CreateAsync(Body(f));
            var id = Id(report);

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{f.EquipmentId}")).StatusCode);
            Assert.Contains(id, await ListIdsAsync($"equipmentId={f.EquipmentId}"));               // not dropped by the join
            Assert.Equal(f.EquipmentId, (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport/{id}"))).GetProperty("equipmentId").GetInt64());

            // an edit that leaves the (now inactive) equipment alone is fine
            var edit = UpdateBodyFrom(await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport/{id}")));
            edit["description"] = "بازبینی شد";
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{id}", edit)).StatusCode);

            // deactivate, idempotent, hidden from the default list, visible with isActive=false
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureReport/{id}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureReport/{id}")).StatusCode);
            Assert.DoesNotContain(id, await ListIdsAsync($"equipmentId={f.EquipmentId}"));
            Assert.Contains(id, await ListIdsAsync($"equipmentId={f.EquipmentId}&isActive=false"));

            // restoring needs an active equipment
            var restore = UpdateBodyFrom(await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport/{id}"))); restore["rowVersion"] = null;
            var blocked = await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{id}", restore);
            Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(blocked)));

            var g = await NewFixtureAsync();
            var other = await CreateAsync(Body(g));
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureReport/{Id(other)}")).StatusCode);
            var back = UpdateBodyFrom(other); back["rowVersion"] = null;
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(other)}", back)).StatusCode);
            Assert.Contains(Id(other), await ListIdsAsync($"equipmentId={g.EquipmentId}"));
        }

        // ------------------------------------------------------------------------------------------ list

        [Fact]
        public async Task The_list_filters_sorts_and_pages()
        {
            var f = await NewFixtureAsync();
            var tag = Guid.NewGuid().ToString("N")[..8];
            var a = Body(f, $"LA-{tag}", 1); a["reportedDateTime"] = "2026-02-01T00:00:00Z";
            var b = Body(f, $"LB-{tag}", 5); b["reportedDateTime"] = "2026-03-01T00:00:00Z";
            var c = Body(f, $"LC-{tag}", 3); c["reportedDateTime"] = "2026-04-01T00:00:00Z";
            var ra = Id(await CreateAsync(a)); var rb = Id(await CreateAsync(b)); var rc = Id(await CreateAsync(c));
            var scope = $"equipmentId={f.EquipmentId}";

            Assert.Equal(new[] { ra, rb, rc }, await ListIdsAsync(scope));
            Assert.Equal(new[] { rb }, await ListIdsAsync($"{scope}&priorityId=5"));
            Assert.Equal(new[] { ra, rc }, await ListIdsAsync($"{scope}&priorityId=1&priorityId=3"));
            Assert.Equal(new[] { rb }, await ListIdsAsync($"{scope}&search=LB-{tag}"));
            Assert.Equal(new[] { rb }, await ListIdsAsync($"{scope}&referenceNumber=LB-{tag}"));
            Assert.Equal(new[] { rb, rc }, await ListIdsAsync($"{scope}&reportedFrom=2026-02-15T00:00:00Z"));
            Assert.Equal(new[] { ra, rb }, await ListIdsAsync($"{scope}&reportedTo=2026-03-15T00:00:00Z"));
            Assert.Equal(new[] { ra, rc, rb }, await ListIdsAsync($"{scope}&sortBy=priority"));
            Assert.Equal(new[] { rc, rb, ra }, await ListIdsAsync($"{scope}&sortBy=reportedDateTime&sortDirection=desc"));
            Assert.Equal(3, (await ListIdsAsync($"failureTypeId={f.FailureTypeId}")).Count);
            Assert.Equal(3, (await ListIdsAsync($"failureCategoryId={f.CategoryId}")).Count);
            Assert.Equal(3, (await ListIdsAsync($"organizationId={f.OrganizationId}&statusId={Open}")).Count);
            Assert.Empty(await ListIdsAsync($"{scope}&statusId={Closed}"));

            var page = await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport?{scope}&pageSize=2&pageNumber=2"));
            Assert.Equal(1, DataOf(page).GetArrayLength());
            Assert.Equal(3, page.GetProperty("meta").GetProperty("pagination").GetProperty("totalCount").GetInt32());

            var bad = await SendAdminAsync(HttpMethod.Get, "/api/FailureReport?priorityId=9&reportedFrom=2026-05-01T00:00:00Z&reportedTo=2026-01-01T00:00:00Z&pageSize=0");
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        }

        // ------------------------------------------------------------------------------------------ dependents & permissions

        [Fact]
        public async Task A_catalog_row_used_by_an_active_report_cannot_be_deactivated()
        {
            var f = await NewFixtureAsync();
            var customType = await PostIdAsync("/api/ReportType", new { title = Uniq("frrt"), code = "CUST" });
            var customStatus = await PostIdAsync("/api/FailureReportStatus", new { title = Uniq("frst"), code = "CUST" });

            var body = Body(f); body["reportTypeId"] = customType;
            var report = await CreateAsync(body);
            var update = UpdateBodyFrom(report); update["failureReportStatusId"] = customStatus;
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/FailureReport/{Id(report)}", update)).StatusCode);

            foreach (var url in new[] { $"/api/ReportType/{customType}", $"/api/FailureReportStatus/{customStatus}", $"/api/FailureType/{f.FailureTypeId}" })
            {
                var blocked = await SendAdminAsync(HttpMethod.Delete, url);
                Assert.True(blocked.StatusCode == HttpStatusCode.Conflict, $"{url} -> {(int)blocked.StatusCode}");
                Assert.Equal("has_active_dependents", ErrorCodeOf(await ReadJsonAsync(blocked)));
            }

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureReport/{Id(report)}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/ReportType/{customType}")).StatusCode);
        }

        [Fact]
        public async Task Every_action_needs_its_permission_and_a_login()
        {
            var admin = await AdminAsync();
            var f = await NewFixtureAsync();
            var report = await CreateAsync(Body(f));
            var userName = "frplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            var id = Id(report);

            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/FailureReport", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, $"/api/FailureReport/{id}", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, "/api/FailureReport", Body(f), bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Put, $"/api/FailureReport/{id}", UpdateBodyFrom(report), bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, $"/api/FailureReport/{id}", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/FailureReport")).StatusCode);

            // a role with only Create can register a report and is the one it is attributed to
            var role = await CreateRoleAsync(admin, "FrCreate" + Guid.NewGuid().ToString("N")[..4], "FailureReports.Create");
            var registrar = await CreateUserAsync(admin, "frreg" + Guid.NewGuid().ToString("N")[..6], "Passw0rd!x");
            await AssignRoleAsync(admin, registrar, role);
            var login = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = (await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/Users/{registrar}", bearer: admin))).GetProperty("userName").GetString(), password = "Passw0rd!x" });
            var token = (await ReadDataAsync(login)).GetProperty("accessToken").GetString()!;
            var created = await SendAsync(HttpMethod.Post, "/api/FailureReport", Body(f), bearer: token);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            Assert.Equal(registrar, (await ReadDataAsync(created)).GetProperty("reportedByUserId").GetInt64());
        }
    }
}
