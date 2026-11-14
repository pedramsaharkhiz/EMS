using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// A failure type applies to the equipment types it is linked to; one linked to none applies to every equipment type. The dropdown of a failure
    /// report asks for the types of the equipment's type (<c>?equipmentTypeId=</c>); nothing refuses a report whose failure type is not linked to its equipment type.
    /// </summary>
    public class FailureTypeEquipmentTypesTests : ApiTestBase
    {
        public FailureTypeEquipmentTypesTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private Task<HttpResponseMessage> AsAdmin(HttpMethod method, string url, object? body = null) => AdminAsync().ContinueWith(t => SendAsync(method, url, body, bearer: t.Result)).Unwrap();
        private Task<long> PostIdAsync(string url, object body) => CatalogFixtures.PostAsync(this, url, body, "id");
        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        private async Task<long> NewEquipmentTypeAsync(string? title = null) =>
            await PostIdAsync("/api/EquipmentType", new { equipmentCategoryId = await PostIdAsync("/api/EquipmentCategory", new { title = Uniq("ftc") }), title = title ?? Uniq("ftt"), code = "T" });

        private long _category;
        private async Task<long> CategoryAsync() => _category != 0 ? _category : _category = await PostIdAsync("/api/FailureCategory", new { title = Uniq("ftfc") });

        private async Task<long> NewFailureTypeAsync(params long[] equipmentTypes)
        {
            var response = await AsAdmin(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = await CategoryAsync(), title = Uniq("ftf"), code = "FT", equipmentTypeIds = equipmentTypes });
            Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
            return Id(await ReadDataAsync(response));
        }

        private async Task<List<long>> DropdownAsync(long equipmentTypeId)
        {
            var response = await AsAdmin(HttpMethod.Get, $"/api/FailureType?equipmentTypeId={equipmentTypeId}&failureCategoryId={await CategoryAsync()}&pageSize=100");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await ReadDataAsync(response)).EnumerateArray().Select(Id).ToList();
        }

        private async Task<JsonElement> DetailAsync(long id) => await ReadDataAsync(await AsAdmin(HttpMethod.Get, $"/api/FailureType/{id}"));
        private static long[] LinkedOf(JsonElement type) => type.GetProperty("equipmentTypes").EnumerateArray().Select(Id).OrderBy(x => x).ToArray();

        [Fact]
        public async Task The_dropdown_of_an_equipment_type_shows_the_types_linked_to_it_and_the_ones_that_apply_to_all()
        {
            var a = await NewEquipmentTypeAsync();
            var b = await NewEquipmentTypeAsync();
            var c = await NewEquipmentTypeAsync();
            var onlyA = await NewFailureTypeAsync(a);
            var aAndB = await NewFailureTypeAsync(a, b);
            var general = await NewFailureTypeAsync();

            Assert.Equal(new[] { onlyA, aAndB, general }, (await DropdownAsync(a)).OrderBy(x => x));
            Assert.Equal(new[] { aAndB, general }, (await DropdownAsync(b)).OrderBy(x => x));
            Assert.Equal(new[] { general }, await DropdownAsync(c));               // linked to none of its types: only what applies to all

            // without the filter the list is unchanged
            var all = (await ReadDataAsync(await AsAdmin(HttpMethod.Get, $"/api/FailureType?failureCategoryId={await CategoryAsync()}&pageSize=100"))).EnumerateArray().Select(Id).ToList();
            Assert.True(new[] { onlyA, aAndB, general }.All(all.Contains));

            // list item and detail say where it applies, with titles
            var item = (await ReadDataAsync(await AsAdmin(HttpMethod.Get, $"/api/FailureType?failureCategoryId={await CategoryAsync()}&pageSize=100"))).EnumerateArray().Single(t => Id(t) == aAndB);
            Assert.Equal(new[] { a, b }.OrderBy(x => x), LinkedOf(item));
            Assert.All(item.GetProperty("equipmentTypes").EnumerateArray(), t => Assert.False(string.IsNullOrEmpty(t.GetProperty("title").GetString())));
            Assert.Equal(new[] { a, b }.OrderBy(x => x), LinkedOf(await DetailAsync(aAndB)));
            Assert.Empty(LinkedOf(await DetailAsync(general)));
        }

        [Fact]
        public async Task The_links_are_set_on_create_replaced_kept_when_omitted_cleared_by_an_empty_list_and_checked()
        {
            var a = await NewEquipmentTypeAsync();
            var b = await NewEquipmentTypeAsync();
            var id = await NewFailureTypeAsync(b, a, b);                          // a repeated id counts once
            Assert.Equal(new[] { a, b }.OrderBy(x => x), LinkedOf(await DetailAsync(id)));

            Task<HttpResponseMessage> Put(object links) => AsAdmin(HttpMethod.Put, $"/api/FailureType/{id}", links);

            // omitted = keep (an older client that does not know the field must not wipe the links)
            Assert.Equal(HttpStatusCode.OK, (await Put(new { title = Uniq("ftf"), isActive = true })).StatusCode);
            Assert.Equal(new[] { a, b }.OrderBy(x => x), LinkedOf(await DetailAsync(id)));

            // a list is the whole new list
            var replaced = await Put(new { title = Uniq("ftf"), isActive = true, equipmentTypeIds = new[] { b } });
            Assert.Equal(HttpStatusCode.OK, replaced.StatusCode);
            Assert.Equal(new[] { b }, LinkedOf(await ReadDataAsync(replaced)));
            Assert.DoesNotContain(id, await DropdownAsync(a));
            Assert.Contains(id, await DropdownAsync(b));

            // an empty list clears: it applies to every equipment type again
            Assert.Equal(HttpStatusCode.OK, (await Put(new { title = Uniq("ftf"), isActive = true, equipmentTypeIds = Array.Empty<long>() })).StatusCode);
            Assert.Empty(LinkedOf(await DetailAsync(id)));
            Assert.Contains(id, await DropdownAsync(a));

            // an unknown equipment type is a 404, an invalid id a 400, and nothing changes
            var unknown = await Put(new { title = Uniq("ftf"), isActive = true, equipmentTypeIds = new long[] { a, 987654321 } });
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));
            Assert.Equal(HttpStatusCode.BadRequest, (await Put(new { title = Uniq("ftf"), isActive = true, equipmentTypeIds = new long[] { 0 } })).StatusCode);
            Assert.Empty(LinkedOf(await DetailAsync(id)));
            Assert.Equal(HttpStatusCode.NotFound, (await AsAdmin(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = await CategoryAsync(), title = Uniq("ftf"), equipmentTypeIds = new long[] { 987654321 } })).StatusCode);
        }

        [Fact]
        public async Task A_deactivated_equipment_type_is_neither_listed_nor_added_but_the_link_survives_its_return()
        {
            var a = await NewEquipmentTypeAsync();
            var b = await NewEquipmentTypeAsync();
            var id = await NewFailureTypeAsync(a, b);

            Assert.Equal(HttpStatusCode.OK, (await AsAdmin(HttpMethod.Delete, $"/api/EquipmentType/{b}")).StatusCode);
            Assert.Equal(new[] { a }, LinkedOf(await DetailAsync(id)));              // only active equipment types are listed

            // a deactivated type cannot be ADDED
            var other = await NewFailureTypeAsync();
            var refused = await AsAdmin(HttpMethod.Put, $"/api/FailureType/{other}", new { title = Uniq("ftf"), isActive = true, equipmentTypeIds = new[] { b } });
            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(refused)));
        }

        [Fact]
        public async Task A_report_whose_failure_type_is_not_linked_to_its_equipment_type_is_still_accepted()
        {
            var equipmentType = await NewEquipmentTypeAsync();
            var otherType = await NewEquipmentTypeAsync();
            var failureType = await NewFailureTypeAsync(otherType);                    // not linked to the equipment's type
            var location = await PostIdAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("ftloc") });
            var equipment = await AsAdmin(HttpMethod.Post, "/api/Equipment", new
            {
                yaasCode = Uniq("FTY"), owner = 1, equipmentTypeId = equipmentType, equipmentStatusId = (long)EquipmentStatusCode.InStock, currentSupportingDepartmentId = location,
            });
            Assert.Equal(HttpStatusCode.Created, equipment.StatusCode);
            var equipmentId = Id(await ReadDataAsync(equipment));

            long organizationId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                var organization = new EMS.Domain.Entities.Organization
                {
                    Title = Uniq("ftorg"),
                    Field = new EMS.Domain.Entities.Field { Title = Uniq("ftfield"), OrganizationManagement = new EMS.Domain.Entities.OrganizationManagement { Title = Uniq("ftmgmt"), Zone = new EMS.Domain.Entities.Zone { Title = Uniq("ftzone") } } },
                    OrganizationType = new EMS.Domain.Entities.OrganizationType { Title = Uniq("ftotype"), CreatedByUserId = 1 },
                };
                db.Organizations.Add(organization);
                await db.SaveChangesAsync();
                organizationId = organization.OrganizationId;
            }

            var report = await AsAdmin(HttpMethod.Post, "/api/FailureReport", new
            {
                equipmentId, failureTypeId = failureType, reportTypeId = (long)ReportTypeCode.Failure, organizationId, priorityId = 3, referenceNumber = Uniq("FTR"),
            });
            Assert.True(report.StatusCode == HttpStatusCode.Created, await report.Content.ReadAsStringAsync());
        }
    }
}
