using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// GET /api/OrganizationHierarchy?searchLevel=employee: the bank employees, each under exactly one branch, with the organizational structure
    /// (جایگاه شغلی) they belong to. Read only; personal data such as the national code and the birth date is not returned.
    /// </summary>
    public class OrganizationHierarchyEmployeeTests : ApiTestBase
    {
        public OrganizationHierarchyEmployeeTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<JsonElement> ListAsync(string query)
        {
            var response = await SendAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?{query}", bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadJsonAsync(response);
        }

        private sealed record Tree(long ZoneId, long ManagementId, long? FieldId, long OrganizationId, string OrganizationTitle);

        /// <summary>A branch under a field (zone > management > field > branch) or directly under a management (zone > management > branch).</summary>
        private async Task<Tree> NewBranchAsync(bool viaField)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var zone = new Zone { Title = Uniq("hzone") };
            var management = new OrganizationManagement { Title = Uniq("hmgmt"), Zone = zone };
            var field = viaField ? new Field { Title = Uniq("hfield"), OrganizationManagement = management } : null;
            var organization = new Organization
            {
                Title = Uniq("horg"), Field = field, OrganizationManagement = viaField ? null : management,
                OrganizationType = new OrganizationType { Title = Uniq("hotype"), CreatedByUserId = 1 },
            };
            db.Organizations.Add(organization);
            await db.SaveChangesAsync();
            return new Tree(zone.ZoneId, management.OrganizationManagementId, field?.FieldId, organization.OrganizationId, organization.Title);
        }

        private async Task<(long Id, long StructureId)> NewEmployeeAsync(long organizationId, string first, string last, OrganizationStructure? structure = null, bool active = true)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            structure ??= new OrganizationStructure { Title = Uniq("hstruct"), OrganizationStructureCode = "S" + Guid.NewGuid().ToString("N")[..6], Grade = 3 };
            var employee = new Employee
            {
                PersonnelCode = Uniq("HE"), FirstName = first, LastName = last, NationalCode = Guid.NewGuid().ToString("N")[..10],
                BirthDate = new DateTime(1990, 1, 1), OrganizationId = organizationId, OrganizationStructure = structure, IsActive = active,
            };
            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return (employee.EmployeeId, employee.OrganizationStructureId);
        }

        private static List<JsonElement> Items(JsonElement envelope) => DataOf(envelope).GetProperty("items").EnumerateArray().ToList();

        [Fact]
        public async Task The_employees_of_a_branch_come_with_their_structure_the_branch_as_parent_and_the_path_above_it()
        {
            var tree = await NewBranchAsync(viaField: true);
            var other = await NewBranchAsync(viaField: true);
            var structure = new OrganizationStructure { Title = "کارشناس", OrganizationStructureCode = "EXP", Grade = 5 };
            var ali = await NewEmployeeAsync(tree.OrganizationId, "علی", "احمدی", structure);
            await NewEmployeeAsync(other.OrganizationId, "رضا", "کریمی");

            var page = await ListAsync($"searchLevel=employee&parentLevel=organization&parentId={tree.OrganizationId}");
            var item = Assert.Single(Items(page));

            Assert.Equal(ali.Id, item.GetProperty("id").GetInt64());
            Assert.Equal("employee", item.GetProperty("level").GetString());
            Assert.Equal("علی احمدی", item.GetProperty("title").GetString());
            Assert.Equal("علی", item.GetProperty("firstName").GetString());
            Assert.Equal("احمدی", item.GetProperty("lastName").GetString());
            Assert.StartsWith("HE", item.GetProperty("code").GetString());                     // the personnel code
            Assert.True(item.GetProperty("isActive").GetBoolean());

            var organizationStructure = item.GetProperty("organizationStructure");
            Assert.Equal(ali.StructureId, organizationStructure.GetProperty("id").GetInt64());
            Assert.Equal("EXP", organizationStructure.GetProperty("code").GetString());
            Assert.Equal("کارشناس", organizationStructure.GetProperty("title").GetString());
            Assert.Equal(5, organizationStructure.GetProperty("grade").GetInt32());

            var parent = item.GetProperty("parent");
            Assert.Equal(tree.OrganizationId, parent.GetProperty("id").GetInt64());
            Assert.Equal("organization", parent.GetProperty("level").GetString());
            Assert.Equal(tree.OrganizationTitle, parent.GetProperty("title").GetString());

            // the zone, the management and the field above the branch, from the top
            var ancestors = item.GetProperty("ancestors").EnumerateArray().Select(a => (a.GetProperty("level").GetString(), a.GetProperty("id").GetInt64())).ToArray();
            Assert.Equal(new[] { ("zone", tree.ZoneId), ("organizationManagement", tree.ManagementId), ("field", tree.FieldId!.Value) }, ancestors);

            // personal data stays out
            foreach (var name in new[] { "nationalCode", "birthDate", "profilePictureUrl" })
                Assert.False(item.TryGetProperty(name, out _), name);
        }

        [Fact]
        public async Task The_other_levels_have_no_employee_fields_and_a_branch_without_a_field_has_no_field_ancestor()
        {
            var direct = await NewBranchAsync(viaField: false);
            await NewEmployeeAsync(direct.OrganizationId, "سارا", "نوری");

            var employee = Assert.Single(Items(await ListAsync($"searchLevel=employee&parentLevel=organization&parentId={direct.OrganizationId}")));
            Assert.Equal(new[] { "zone", "organizationManagement" },
                employee.GetProperty("ancestors").EnumerateArray().Select(a => a.GetProperty("level").GetString()).ToArray());

            var branch = Assert.Single(Items(await ListAsync($"searchLevel=organization&parentLevel=organizationManagement&parentId={direct.ManagementId}")));
            Assert.Equal(JsonValueKind.Null, branch.GetProperty("organizationStructure").ValueKind);
            Assert.Equal(JsonValueKind.Null, branch.GetProperty("firstName").ValueKind);
        }

        [Fact]
        public async Task Employees_can_be_found_under_any_level_above_the_branch()
        {
            var tree = await NewBranchAsync(viaField: true);
            var second = await NewBranchAsync(viaField: true);
            var a = await NewEmployeeAsync(tree.OrganizationId, "آ", "اول");
            await NewEmployeeAsync(second.OrganizationId, "ب", "دوم");

            foreach (var (level, id) in new[] { ("field", tree.FieldId!.Value), ("organizationManagement", tree.ManagementId), ("zone", tree.ZoneId) })
            {
                var items = Items(await ListAsync($"searchLevel=employee&parentLevel={level}&parentId={id}"));
                Assert.Equal(new[] { a.Id }, items.Select(i => i.GetProperty("id").GetInt64()).ToArray());
            }
        }

        [Fact]
        public async Task Filters_search_and_order_apply_to_employees()
        {
            var tree = await NewBranchAsync(viaField: true);
            var zahra = await NewEmployeeAsync(tree.OrganizationId, "زهرا", "بهرامی");
            var ali = await NewEmployeeAsync(tree.OrganizationId, "علی", "احمدی");
            var inactive = await NewEmployeeAsync(tree.OrganizationId, "حسین", "ثابتی", active: false);
            var parent = $"parentLevel=organization&parentId={tree.OrganizationId}";

            // by last name, then first name
            Assert.Equal(new[] { ali.Id, zahra.Id, inactive.Id },
                Items(await ListAsync($"searchLevel=employee&{parent}")).Select(i => i.GetProperty("id").GetInt64()).ToArray());

            Assert.Equal(new[] { ali.Id }, Items(await ListAsync($"searchLevel=employee&{parent}&search=احمدی")).Select(i => i.GetProperty("id").GetInt64()).ToArray());
            Assert.Equal(new[] { zahra.Id }, Items(await ListAsync($"searchLevel=employee&{parent}&title=زهرا")).Select(i => i.GetProperty("id").GetInt64()).ToArray());
            Assert.Equal(new[] { inactive.Id }, Items(await ListAsync($"searchLevel=employee&{parent}&isActive=false")).Select(i => i.GetProperty("id").GetInt64()).ToArray());

            var code = Items(await ListAsync($"searchLevel=employee&{parent}&search=احمدی"))[0].GetProperty("code").GetString();
            Assert.Equal(new[] { ali.Id }, Items(await ListAsync($"searchLevel=employee&{parent}&code={code}")).Select(i => i.GetProperty("id").GetInt64()).ToArray());

            var paged = await ListAsync($"searchLevel=employee&{parent}&pageSize=2&pageNumber=2");
            Assert.Equal(3, PaginationOf(paged).GetProperty("totalCount").GetInt32());
            Assert.Single(Items(paged));
        }

        [Fact]
        public async Task A_parent_below_the_employee_or_a_branch_filter_on_another_level_is_refused_and_the_permission_is_needed()
        {
            var tree = await NewBranchAsync(viaField: true);

            foreach (var query in new[] { "searchLevel=organization&parentLevel=employee&parentId=1", "searchLevel=employee&parentLevel=employee&parentId=1", "searchLevel=employee&supportingDepartmentId=1" })
            {
                var response = await SendAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?{query}", bearer: await AdminAsync());
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(response)));
            }

            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?searchLevel=employee&parentLevel=organization&parentId={tree.OrganizationId}")).StatusCode);
        }
    }
}
