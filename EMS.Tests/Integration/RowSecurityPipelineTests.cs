using EMS.Application.Common.DataAccess;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Security.RowSecurity;
using EMS.Tests.Support;
using ClosedXML.Excel;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The row-security machinery end to end: scope resolver -> central door -> policy -> the very queries behind the lists, the Excel files and the dashboard.
    /// A stand-in rule (department) is switched on for the test only (RowSecurityTestState); in production nothing restricts anybody yet.
    /// </summary>
    public class RowSecurityPipelineTests : ApiTestBase
    {
        public RowSecurityPipelineTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private async Task<HttpResponseMessage> AsAdminAsync(HttpMethod method, string url, object? body = null) => await SendAsync(method, url, body, bearer: await AdminAsync());
        private Task<long> PostIdAsync(string url, object body) => CatalogFixtures.PostAsync(this, url, body, "id");
        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        private async Task<int> TotalAsync(string url)
        {
            var response = await AsAdminAsync(HttpMethod.Get, url + (url.Contains('?') ? "&" : "?") + "pageSize=1");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return PaginationOf(await ReadJsonAsync(response)).GetProperty("totalCount").GetInt32();
        }

        // ------------------------------------------------------------------------------------------ the explanation endpoint

        [Fact]
        public async Task My_scope_tells_a_signed_in_user_how_much_he_sees_and_lists_every_protected_type()
        {
            var response = await AsAdminAsync(HttpMethod.Get, "/api/RowSecurity/my-scope");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var scope = await ReadDataAsync(response);

            Assert.True(scope.GetProperty("isUnrestricted").GetBoolean());                      // production default: nobody is restricted yet
            Assert.Equal((await LoginAdminAsync()).UserId, scope.GetProperty("userId").GetInt64());
            Assert.False(string.IsNullOrWhiteSpace(scope.GetProperty("reason").GetString()));
            var policies = scope.GetProperty("policies").EnumerateArray().ToList();
            foreach (var type in new[] { "EquipmentRow", "FailureReportRow", "TechnicianAssignmentRow", "ApplicationUser" })
                Assert.Contains(policies, p => p.GetProperty("rowType").GetString() == type);
            Assert.All(policies, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.GetProperty("description").GetString()));
                Assert.False(p.GetProperty("restrictsYou").GetBoolean());
            });

            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/RowSecurity/my-scope")).StatusCode);
        }

        // ------------------------------------------------------------------------------------------ structure

        [Fact]
        public void Every_protected_type_has_a_registered_policy_and_the_policy_names_are_unique()
        {
            using var scope = _factory.Services.CreateScope();
            var services = scope.ServiceProvider;

            foreach (var rowType in new[] { typeof(EquipmentRow), typeof(FailureReportRow), typeof(TechnicianAssignmentRow), typeof(ApplicationUser) })
                Assert.NotEmpty(services.GetServices(typeof(IRowPolicy<>).MakeGenericType(rowType)));

            var descriptors = services.GetServices<IRowPolicyDescriptor>().ToList();
            Assert.Equal(descriptors.Count, descriptors.Select(d => d.Name).Distinct().Count());
            Assert.All(descriptors, d => Assert.False(string.IsNullOrWhiteSpace(d.Name)));
            // every policy of the application is also reachable through the explanation screen
            var all = typeof(IRowPolicy<>).Assembly.GetTypes().Concat(typeof(UserRowPolicy).Assembly.GetTypes())
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRowPolicy<>)))
                .Distinct().ToList();
            Assert.All(all, t => Assert.Contains(descriptors, d => d.GetType() == t));
        }

        // ------------------------------------------------------------------------------------------ the pipeline

        [Fact]
        public async Task A_restricted_scope_limits_the_lists_the_excel_files_and_the_dashboard_with_one_rule_and_a_normal_scope_is_unaffected()
        {
            var tag = Uniq("rs");
            var departmentA = await PostIdAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("rsA") });
            var departmentB = await PostIdAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("rsB") });
            var organizationA = await NewOrganizationAsync(departmentA);
            var organizationB = await NewOrganizationAsync(departmentB);
            var typeId = await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("rsc") }), title = Uniq("rst"), code = "T" });

            async Task<long> EquipmentAtAsync(long department)
            {
                var response = await AsAdminAsync(HttpMethod.Post, "/api/Equipment", new
                {
                    yaasCode = Uniq("RSY"), owner = 2, equipmentTypeId = typeId, equipmentStatusId = (long)EquipmentStatusCode.UnderMaintenance, currentSupportingDepartmentId = department
                });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                return Id(await ReadDataAsync(response));
            }

            var failureType = await PostIdAsync("/api/FailureType", new { failureCategoryId = await PostIdAsync("/api/FailureCategory", new { title = Uniq("rsfc") }), title = Uniq("rsft"), code = "FT" });
            async Task<long> ReportAtAsync(long organization, long equipment, string reference)
            {
                var response = await AsAdminAsync(HttpMethod.Post, "/api/FailureReport", new
                {
                    equipmentId = equipment, failureTypeId = failureType, reportTypeId = (long)ReportTypeCode.Failure, organizationId = organization, priorityId = 3, referenceNumber = reference
                });
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                return Id(await ReadDataAsync(response));
            }

            var equipmentA1 = await EquipmentAtAsync(departmentA);
            var equipmentA2 = await EquipmentAtAsync(departmentA);
            var equipmentB = await EquipmentAtAsync(departmentB);
            var reportA1 = await ReportAtAsync(organizationA, equipmentB, $"{tag}-A1");
            var reportA2 = await ReportAtAsync(organizationA, equipmentB, $"{tag}-A2");
            var reportB = await ReportAtAsync(organizationB, equipmentB, $"{tag}-B1");

            var unrestrictedReports = await TotalAsync("/api/FailureReport");
            var unrestrictedEquipment = await TotalAsync("/api/Equipment");
            var dashboardEquipmentBefore = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/Dashboard/equipment"))).GetProperty("total").GetInt32();
            Assert.True(unrestrictedReports >= 3 && unrestrictedEquipment >= 3);

            RowSecurityTestState.RestrictedToDepartment = departmentA;
            try
            {
                // the explanation says so
                var scope = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/RowSecurity/my-scope"));
                Assert.False(scope.GetProperty("isUnrestricted").GetBoolean());
                Assert.Equal(departmentA, scope.GetProperty("supportingDepartmentId").GetInt64());
                var restricting = scope.GetProperty("policies").EnumerateArray().Where(p => p.GetProperty("restrictsYou").GetBoolean()).Select(p => p.GetProperty("name").GetString()).OrderBy(n => n);
                Assert.Equal(new[] { "TestEquipmentDepartment", "TestFailureReportDepartment" }, restricting);

                // failure reports: the list, the file and the dashboard card all show exactly the two reports of department A
                var listed = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/FailureReport?pageSize=100&sortDirection=desc"))).EnumerateArray().Select(Id).ToList();
                Assert.Equal(new[] { reportA2, reportA1 }, listed);
                Assert.Equal(2, await TotalAsync("/api/FailureReport"));
                Assert.DoesNotContain(reportB, listed);

                var export = await AsAdminAsync(HttpMethod.Get, "/api/FailureReport/export");
                Assert.Equal(HttpStatusCode.OK, export.StatusCode);
                using (var book = new XLWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync())))
                    Assert.Equal(3, book.Worksheet(1).LastRowUsed()!.RowNumber());                  // header + the two reports

                var card = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/Dashboard/failure-reports"));
                Assert.Equal(2, card.GetProperty("total").GetInt32());
                Assert.Equal(2, card.GetProperty("unresolvedCount").GetInt32());

                // equipment: the two held by department A (the branch-based part of the rule has no data here)
                Assert.Equal(2, await TotalAsync("/api/Equipment"));
                var equipmentCard = await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/Dashboard/equipment"));
                Assert.Equal(2, equipmentCard.GetProperty("total").GetInt32());
                Assert.Equal(2, equipmentCard.GetProperty("needsActionCount").GetInt32());
                Assert.Equal(2, equipmentCard.GetProperty("byStatus").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));
                Assert.Equal(2, equipmentCard.GetProperty("byOwner").EnumerateArray().Sum(b => b.GetProperty("count").GetInt32()));
                _ = (equipmentA1, equipmentA2);
            }
            finally
            {
                RowSecurityTestState.RestrictedToDepartment = null;
            }

            // switched off again: everything is back, nothing was changed in the data
            Assert.Equal(unrestrictedReports, await TotalAsync("/api/FailureReport"));
            Assert.Equal(unrestrictedEquipment, await TotalAsync("/api/Equipment"));
            Assert.Equal(dashboardEquipmentBefore, (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, "/api/Dashboard/equipment"))).GetProperty("total").GetInt32());
        }

        private async Task<long> NewOrganizationAsync(long departmentId)
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
    }
}
