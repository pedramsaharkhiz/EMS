using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>The branches a supporting department serves: assign / remove one by one and by group, conflicts, filters.</summary>
    public class SupportingDepartmentScopeTests : ApiTestBase
    {
        public SupportingDepartmentScopeTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<long> NewDepartmentAsync()
        {
            var province = await CatalogFixtures.PostAsync(this, "/api/Province", new { title = Uniq("sdprov") }, "id");
            var city = await CatalogFixtures.PostAsync(this, "/api/City", new { provinceId = province, title = Uniq("sdcity") }, "id");
            return await CatalogFixtures.PostAsync(this, "/api/SupportingDepartment", new { cityId = city, title = Uniq("sddept") }, "id");
        }

        /// <summary>
        /// Zone → Management → Field → { a, b } and, directly under the management, { c } (no field); plus one inactive branch under the field.
        /// </summary>
        private sealed record Tree(long ZoneId, long ManagementId, long FieldId, long A, long B, long C, long InactiveUnderField);

        private async Task<Tree> NewTreeAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var zone = new Zone { Title = Uniq("sdzone") };
            var management = new OrganizationManagement { Title = Uniq("sdmgmt"), Zone = zone };
            var field = new Field { Title = Uniq("sdfield"), OrganizationManagement = management };
            var type = new OrganizationType { Title = Uniq("sdotype"), CreatedByUserId = 1 };
            var a = new Organization { Title = Uniq("sdA"), Field = field, OrganizationType = type };
            var b = new Organization { Title = Uniq("sdB"), Field = field, OrganizationType = type };
            var c = new Organization { Title = Uniq("sdC"), OrganizationManagement = management, OrganizationType = type };
            var inactive = new Organization { Title = Uniq("sdX"), Field = field, OrganizationType = type, IsActive = false };
            db.Organizations.AddRange(a, b, c, inactive);
            await db.SaveChangesAsync();
            return new Tree(zone.ZoneId, management.OrganizationManagementId, field.FieldId, a.OrganizationId, b.OrganizationId, c.OrganizationId, inactive.OrganizationId);
        }

        private async Task<JsonElement> AssignAsync(long department, object body, HttpStatusCode expected = HttpStatusCode.OK)
        {
            var response = await SendAdminAsync(HttpMethod.Post, $"/api/SupportingDepartment/{department}/organizations", body);
            Assert.True(response.StatusCode == expected, $"assign -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadJsonAsync(response);
        }

        private async Task<List<long>> BranchesOfAsync(long department)
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?searchLevel=organization&supportingDepartmentId={department}&pageSize=100");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return DataOf(await ReadJsonAsync(response)).GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).OrderBy(x => x).ToList();
        }

        private static List<long> Sorted(params long[] ids) => ids.OrderBy(x => x).ToList();

        // ------------------------------------------------------------------------------------ assign

        [Fact]
        public async Task Branches_can_be_assigned_one_by_one_and_show_up_everywhere()
        {
            var department = await NewDepartmentAsync();
            var tree = await NewTreeAsync();

            var json = await AssignAsync(department, new { organizationIds = new[] { tree.A, tree.C } });
            var result = DataOf(json);
            Assert.Equal(2, result.GetProperty("selected").GetInt32());
            Assert.Equal(2, result.GetProperty("assigned").GetInt32());
            Assert.Equal(0, result.GetProperty("moved").GetInt32());
            Assert.Equal(2, result.GetProperty("totalOrganizations").GetInt32());

            Assert.Equal(Sorted(tree.A, tree.C), await BranchesOfAsync(department));

            // the department list and detail count its active branches
            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment/{department}"));
            Assert.Equal(2, detail.GetProperty("organizationCount").GetInt32());
            var list = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, "/api/SupportingDepartment?pageSize=100")));
            Assert.Equal(2, list.EnumerateArray().Single(d => d.GetProperty("id").GetInt64() == department).GetProperty("organizationCount").GetInt32());

            // the branch row carries its department
            var row = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?searchLevel=organization&supportingDepartmentId={department}&pageSize=100")))
                .GetProperty("items")[0];
            Assert.Equal(department, row.GetProperty("supportingDepartmentId").GetInt64());
            Assert.False(string.IsNullOrEmpty(row.GetProperty("supportingDepartmentTitle").GetString()));

            // repeating it changes nothing
            var again = DataOf(await AssignAsync(department, new { organizationIds = new[] { tree.A } }));
            Assert.Equal(1, again.GetProperty("unchanged").GetInt32());
            Assert.Equal(0, again.GetProperty("assigned").GetInt32());
        }

        [Fact]
        public async Task A_field_a_management_or_a_zone_selects_all_its_active_branches()
        {
            var tree = await NewTreeAsync();

            var byField = await NewDepartmentAsync();
            Assert.Equal(2, DataOf(await AssignAsync(byField, new { fieldIds = new[] { tree.FieldId } })).GetProperty("assigned").GetInt32());
            Assert.Equal(Sorted(tree.A, tree.B), await BranchesOfAsync(byField));           // not the inactive one, not the one outside the field

            var byManagement = await NewDepartmentAsync();
            var moved = DataOf(await AssignAsync(byManagement, new { organizationManagementIds = new[] { tree.ManagementId }, moveFromOtherDepartments = true }));
            Assert.Equal(3, moved.GetProperty("selected").GetInt32());                       // A, B (under the field) and C (directly under the management)
            Assert.Equal(1, moved.GetProperty("assigned").GetInt32());
            Assert.Equal(2, moved.GetProperty("moved").GetInt32());
            Assert.Equal(Sorted(tree.A, tree.B, tree.C), await BranchesOfAsync(byManagement));
            Assert.Empty(await BranchesOfAsync(byField));

            var byZone = await NewDepartmentAsync();
            var zone = DataOf(await AssignAsync(byZone, new { zoneIds = new[] { tree.ZoneId }, moveFromOtherDepartments = true }));
            Assert.Equal(3, zone.GetProperty("selected").GetInt32());
            Assert.Equal(Sorted(tree.A, tree.B, tree.C), await BranchesOfAsync(byZone));
        }

        [Fact]
        public async Task A_selection_is_the_union_and_counts_each_branch_once()
        {
            var department = await NewDepartmentAsync();
            var tree = await NewTreeAsync();

            var result = DataOf(await AssignAsync(department, new { organizationIds = new[] { tree.A, tree.A }, fieldIds = new[] { tree.FieldId }, zoneIds = new[] { tree.ZoneId } }));
            Assert.Equal(3, result.GetProperty("selected").GetInt32());
            Assert.Equal(3, result.GetProperty("assigned").GetInt32());
        }

        [Fact]
        public async Task A_branch_of_another_department_is_a_conflict_that_changes_nothing_unless_moving_is_allowed()
        {
            var first = await NewDepartmentAsync();
            var second = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            await AssignAsync(first, new { organizationIds = new[] { tree.A } });

            var json = await AssignAsync(second, new { organizationIds = new[] { tree.A, tree.B } }, HttpStatusCode.Conflict);
            Assert.Equal("organizations_already_assigned", ErrorCodeOf(json));
            var details = json.GetProperty("error").GetProperty("details").EnumerateArray().ToList();
            Assert.Single(details);
            Assert.Contains("از قبل زیر دپارتمان", details[0].GetProperty("message").GetString());
            Assert.Equal(Sorted(tree.A), await BranchesOfAsync(first));
            Assert.Empty(await BranchesOfAsync(second));                                      // B was not assigned either

            var moved = DataOf(await AssignAsync(second, new { organizationIds = new[] { tree.A, tree.B }, moveFromOtherDepartments = true }));
            Assert.Equal(1, moved.GetProperty("moved").GetInt32());
            Assert.Equal(1, moved.GetProperty("assigned").GetInt32());
            Assert.Empty(await BranchesOfAsync(first));
            Assert.Equal(Sorted(tree.A, tree.B), await BranchesOfAsync(second));
        }

        [Fact]
        public async Task Invalid_selections_are_refused_per_item_and_nothing_is_saved()
        {
            var department = await NewDepartmentAsync();
            var tree = await NewTreeAsync();

            var empty = await SendAdminAsync(HttpMethod.Post, $"/api/SupportingDepartment/{department}/organizations", new { });
            Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
            Assert.Equal("validation_failed", ErrorCodeOf(await ReadJsonAsync(empty)));

            var zero = await SendAdminAsync(HttpMethod.Post, $"/api/SupportingDepartment/{department}/organizations", new { organizationIds = new[] { 0 } });
            Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);

            var json = await AssignAsync(department,
                new { organizationIds = new[] { tree.A, 99999999L, tree.InactiveUnderField }, fieldIds = new[] { 99999999L }, organizationManagementIds = new[] { 99999999L }, zoneIds = new[] { 99999999L } },
                HttpStatusCode.BadRequest);
            Assert.Equal("bulk_items_invalid", ErrorCodeOf(json));
            var fields = ErrorFieldsOf(json);
            foreach (var field in new[] { "organizationIds[1]", "organizationIds[2]", "fieldIds[0]", "organizationManagementIds[0]", "zoneIds[0]" })
                Assert.Contains(field, fields);
            Assert.DoesNotContain("organizationIds[0]", fields);
            Assert.Empty(await BranchesOfAsync(department));                                  // the valid branch was not saved
        }

        [Fact]
        public async Task The_department_must_exist_and_be_active_to_receive_branches()
        {
            var tree = await NewTreeAsync();
            var unknown = await SendAdminAsync(HttpMethod.Post, "/api/SupportingDepartment/99999999/organizations", new { organizationIds = new[] { tree.A } });
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

            var department = await NewDepartmentAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}")).StatusCode);
            var inactive = await SendAdminAsync(HttpMethod.Post, $"/api/SupportingDepartment/{department}/organizations", new { organizationIds = new[] { tree.A } });
            Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(inactive)));
        }

        // ------------------------------------------------------------------------------------ remove

        [Fact]
        public async Task Remove_takes_only_the_departments_own_branches_off()
        {
            var first = await NewDepartmentAsync();
            var second = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            await AssignAsync(first, new { organizationIds = new[] { tree.A, tree.C } });
            await AssignAsync(second, new { organizationIds = new[] { tree.B } });

            var response = await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{first}/organizations", new { fieldIds = new[] { tree.FieldId }, organizationIds = new[] { tree.C } });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await ReadDataAsync(response);
            Assert.Equal(2, result.GetProperty("removed").GetInt32());                         // A and C
            Assert.Equal(1, result.GetProperty("unchanged").GetInt32());                       // B belongs to the other department
            Assert.Equal(0, result.GetProperty("totalOrganizations").GetInt32());
            Assert.Empty(await BranchesOfAsync(first));
            Assert.Equal(Sorted(tree.B), await BranchesOfAsync(second));
        }

        [Fact]
        public async Task A_department_that_serves_active_branches_cannot_be_deactivated()
        {
            var department = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            await AssignAsync(department, new { organizationIds = new[] { tree.A } });

            var blocked = await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}");
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Equal("has_active_dependents", ErrorCodeOf(await ReadJsonAsync(blocked)));

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}/organizations", new { organizationIds = new[] { tree.A } })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}")).StatusCode);
        }

        // ------------------------------------------------------------------------------------ filters

        [Fact]
        public async Task The_branch_list_filters_by_department_and_by_no_department()
        {
            var department = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            await AssignAsync(department, new { organizationIds = new[] { tree.A } });

            var without = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?searchLevel=organization&parentLevel=field&parentId={tree.FieldId}&withoutSupportingDepartment=true&pageSize=100")))
                .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt64()).ToList();
            Assert.DoesNotContain(tree.A, without);
            Assert.Contains(tree.B, without);

            // only valid at the branch level, and the two filters do not mix
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?searchLevel=zone&supportingDepartmentId={department}")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Get, $"/api/OrganizationHierarchy?searchLevel=organization&supportingDepartmentId={department}&withoutSupportingDepartment=true")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Get, "/api/OrganizationHierarchy?searchLevel=organization&supportingDepartmentId=0")).StatusCode);
        }

        [Fact]
        public async Task Failure_reports_filter_by_the_department_of_their_branch_and_show_it()
        {
            var department = await NewDepartmentAsync();
            var other = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            await AssignAsync(department, new { organizationIds = new[] { tree.A } });
            await AssignAsync(other, new { organizationIds = new[] { tree.B } });

            var category = await CatalogFixtures.PostAsync(this, "/api/FailureCategory", new { title = Uniq("sdfc") }, "id");
            var failureType = await CatalogFixtures.PostAsync(this, "/api/FailureType", new { failureCategoryId = category, title = Uniq("sdft") }, "id");
            var equipmentCategory = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("sdec") }, "id");
            var equipmentType = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = equipmentCategory, title = Uniq("sdet"), code = "T" }, "id");
            var location = await CatalogFixtures.NewDepartmentAsync(this);
            var equipment = (await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/Equipment", new { yaasCode = Uniq("SDY"), owner = 1, equipmentTypeId = equipmentType, equipmentStatusId = 1, currentSupportingDepartmentId = location }))).GetProperty("id").GetInt64();

            async Task<long> ReportAsync(long organization) =>
                (await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/FailureReport", new { equipmentId = equipment, failureTypeId = failureType, reportTypeId = 1, organizationId = organization, priorityId = 2 }))).GetProperty("id").GetInt64();

            var inDepartment = await ReportAsync(tree.A);
            var inOther = await ReportAsync(tree.B);
            var noDepartment = await ReportAsync(tree.C);

            async Task<List<long>> ListAsync(string query) =>
                DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport?equipmentId={equipment}&{query}"))).EnumerateArray().Select(r => r.GetProperty("id").GetInt64()).ToList();

            Assert.Equal(new[] { inDepartment }, await ListAsync($"supportingDepartmentId={department}"));
            Assert.Equal(new[] { inDepartment, inOther }, await ListAsync($"supportingDepartmentId={department}&supportingDepartmentId={other}"));
            Assert.Equal(3, (await ListAsync("")).Count);

            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport/{inDepartment}"));
            Assert.Equal(department, detail.GetProperty("supportingDepartmentId").GetInt64());
            Assert.False(string.IsNullOrEmpty(detail.GetProperty("supportingDepartmentTitle").GetString()));
            Assert.Equal(JsonValueKind.Null, (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReport/{noDepartment}"))).GetProperty("supportingDepartmentId").ValueKind);
        }

        // ------------------------------------------------------------------------------------ with the department's own create / update

        private async Task<long> NewCityAsync()
        {
            var province = await CatalogFixtures.PostAsync(this, "/api/Province", new { title = Uniq("sdprov") }, "id");
            return await CatalogFixtures.PostAsync(this, "/api/City", new { provinceId = province, title = Uniq("sdcity") }, "id");
        }

        private async Task<HttpResponseMessage> CreateDepartmentAsync(long city, string title, object? organizations = null) =>
            await SendAdminAsync(HttpMethod.Post, "/api/SupportingDepartment", organizations is null ? new { cityId = city, title } : (object)new { cityId = city, title, organizations });

        private async Task<int> CountByTitleAsync(string title) =>
            DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment?title={title}&isActive=true"))).GetArrayLength();

        [Fact]
        public async Task A_department_can_be_created_with_no_branches_with_an_empty_selection_or_with_branches()
        {
            var city = await NewCityAsync();

            var plain = await CreateDepartmentAsync(city, Uniq("none"));
            Assert.Equal(HttpStatusCode.Created, plain.StatusCode);
            Assert.Equal(0, (await ReadDataAsync(plain)).GetProperty("organizationCount").GetInt32());

            var empty = await CreateDepartmentAsync(city, Uniq("empty"), new { });
            Assert.Equal(HttpStatusCode.Created, empty.StatusCode);
            Assert.Equal(0, (await ReadDataAsync(empty)).GetProperty("organizationCount").GetInt32());

            var tree = await NewTreeAsync();
            var created = await CreateDepartmentAsync(city, Uniq("with"), new { organizationIds = new[] { tree.A }, fieldIds = new[] { tree.FieldId }, organizationManagementIds = new[] { tree.ManagementId } });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var json = await ReadDataAsync(created);
            Assert.Equal(3, json.GetProperty("organizationCount").GetInt32());                  // A, B (field) and C (directly under the management)
            Assert.Equal(Sorted(tree.A, tree.B, tree.C), await BranchesOfAsync(json.GetProperty("id").GetInt64()));
        }

        [Fact]
        public async Task A_failed_branch_selection_stops_the_department_from_being_created()
        {
            var city = await NewCityAsync();
            var other = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            await AssignAsync(other, new { organizationIds = new[] { tree.A } });

            var badId = Uniq("badid");
            var refused = await CreateDepartmentAsync(city, badId, new { organizationIds = new[] { tree.B, 99999999L } });
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            var json = await ReadJsonAsync(refused);
            Assert.Equal("bulk_items_invalid", ErrorCodeOf(json));
            Assert.Contains("organizations.organizationIds[1]", ErrorFieldsOf(json));
            Assert.Equal(0, await CountByTitleAsync(badId));

            var conflictTitle = Uniq("conf");
            var conflict = await CreateDepartmentAsync(city, conflictTitle, new { organizationIds = new[] { tree.A, tree.B } });
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            var conflictJson = await ReadJsonAsync(conflict);
            Assert.Equal("organizations_already_assigned", ErrorCodeOf(conflictJson));
            Assert.Contains("organizations.organizations", ErrorFieldsOf(conflictJson));
            Assert.Equal(0, await CountByTitleAsync(conflictTitle));
            Assert.Equal(Sorted(tree.A), await BranchesOfAsync(other));                          // untouched

            // moving is explicit
            var movedTitle = Uniq("moved");
            var moved = await CreateDepartmentAsync(city, movedTitle, new { organizationIds = new[] { tree.A, tree.B }, moveFromOtherDepartments = true });
            Assert.Equal(HttpStatusCode.Created, moved.StatusCode);
            Assert.Empty(await BranchesOfAsync(other));
            Assert.Equal(Sorted(tree.A, tree.B), await BranchesOfAsync((await ReadDataAsync(moved)).GetProperty("id").GetInt64()));
        }

        [Fact]
        public async Task Updating_a_department_adds_branches_without_taking_any_off_and_leaving_the_field_out_changes_none()
        {
            var tree = await NewTreeAsync();
            var city = await NewCityAsync();
            var created = await ReadDataAsync(await CreateDepartmentAsync(city, Uniq("upd"), new { organizationIds = new[] { tree.A } }));
            var id = created.GetProperty("id").GetInt64();
            var title = created.GetProperty("title").GetString();

            // no "organizations": only the name changes
            var renamed = Uniq("renamed");
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{id}", new { title = renamed, isActive = true })).StatusCode);
            Assert.Equal(Sorted(tree.A), await BranchesOfAsync(id));

            // "organizations": B is added, A stays
            var added = await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{id}", new { title = renamed, isActive = true, organizations = new { organizationIds = new[] { tree.B } } });
            Assert.Equal(HttpStatusCode.OK, added.StatusCode);
            Assert.Equal(2, (await ReadDataAsync(added)).GetProperty("organizationCount").GetInt32());
            Assert.Equal(Sorted(tree.A, tree.B), await BranchesOfAsync(id));

            // a branch that is already here is not a conflict; one of another department is, and then nothing at all changes (not even the title)
            var other = await NewDepartmentAsync();
            await AssignAsync(other, new { organizationIds = new[] { tree.C } });
            var conflictTitle = Uniq("shouldnot");
            var conflict = await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{id}", new { title = conflictTitle, isActive = true, organizations = new { organizationIds = new[] { tree.A, tree.C } } });
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("organizations_already_assigned", ErrorCodeOf(await ReadJsonAsync(conflict)));
            Assert.Equal(renamed, (await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment/{id}"))).GetProperty("title").GetString());
            Assert.Equal(Sorted(tree.C), await BranchesOfAsync(other));

            // deactivating together with new branches makes no sense
            var both = await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{id}", new { title = renamed, isActive = false, organizations = new { organizationIds = new[] { tree.B } } });
            Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
            Assert.Contains("organizations", ErrorFieldsOf(await ReadJsonAsync(both)));
            Assert.NotNull(title);
        }

        // ------------------------------------------------------------------------------------ permissions

        [Fact]
        public async Task Assigning_and_removing_need_the_update_permission()
        {
            var admin = await AdminAsync();
            var department = await NewDepartmentAsync();
            var tree = await NewTreeAsync();
            var userName = "sdplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            var body = new { organizationIds = new[] { tree.A } };
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, $"/api/SupportingDepartment/{department}/organizations", body, bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}/organizations", body, bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Post, $"/api/SupportingDepartment/{department}/organizations", body)).StatusCode);
        }
    }
}
