using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>The soft-delete query filter, and the dependents that must keep a record from being deactivated.</summary>
    public class SoftDeleteFilterTests : ApiTestBase
    {
        public SoftDeleteFilterTests(EmsApiFactory factory) : base(factory) { }

        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<string> AdminAsync() => (await LoginAdminAsync()).AccessToken;

        private async Task<long> PostAsync(string url, object body, string idProperty)
        {
            var response = await SendAsync(HttpMethod.Post, url, body, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty(idProperty).GetInt64();
        }

        private Task<long> NewStatusAsync() => PostAsync("/api/EquipmentStatus", new { title = Uniq("sd-st"), code = "S" }, "id");

        private async Task<long> NewTypeAsync() =>
            await PostAsync("/api/EquipmentType", new
            {
                equipmentCategoryId = await PostAsync("/api/EquipmentCategory", new { title = Uniq("sd-cat") }, "id"),
                title = Uniq("sd-type"),
                code = "T"
            }, "id");

        private async Task<long> NewModelAsync() =>
            await PostAsync("/api/EquipmentModel", new
            {
                equipmentTypeId = await NewTypeAsync(),
                brandId = await PostAsync("/api/Brand", new { title = Uniq("sd-br"), code = "B" }, "id"),
                title = Uniq("sd-model"),
                code = "M"
            }, "id");

        private async Task<long> NewDepartmentAsync() =>
            await PostAsync("/api/SupportingDepartment", new { cityId = await CatalogFixtures.EnsureCityAsync(this), title = Uniq("sd-dept") }, "id");

        private async Task<long> InsertEquipmentAsync(long statusId, long departmentId, long? typeId = null, long? modelId = null, bool active = true)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            // the database requires exactly one of type / model
            var equipment = new Equipment
            {
                YaasCode = Uniq("YC"),
                EquipmentStatusId = statusId,
                EquipmentTypeId = modelId is null ? typeId : null,
                EquipmentModelId = modelId,
                CurrentSupportingDepartmentId = departmentId,
                IsActive = active
            };
            db.Equipments.Add(equipment);
            await db.SaveChangesAsync();
            return equipment.EquipmentId;
        }

        private async Task SetEquipmentActiveAsync(long equipmentId, bool active)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var equipment = await db.Equipments.SingleAsync(e => e.EquipmentId == equipmentId);
            equipment.IsActive = active;
            await db.SaveChangesAsync();
        }

        // ------------------------------------------------------------------------------- the filter itself

        [Fact]
        public void Every_catalog_entity_has_the_soft_delete_filter_so_nobody_can_forget_it()
        {
            using var scope = _factory.Services.CreateScope();
            var model = scope.ServiceProvider.GetRequiredService<EMSDbContext>().Model;

            var catalogs = model.GetEntityTypes().Where(t => typeof(ICatalogEntity).IsAssignableFrom(t.ClrType)).ToList();
            Assert.True(catalogs.Count >= 8);
            foreach (var entityType in catalogs)
            {
                Assert.True(
                    entityType.GetDeclaredQueryFilters().Any(f => f.Key == SoftDelete.FilterName),
                    $"{entityType.ClrType.Name} implements ICatalogEntity but has no soft-delete filter");
            }
        }

        [Fact]
        public async Task A_plain_query_never_sees_deactivated_catalog_rows_but_including_inactive_does()
        {
            var admin = await AdminAsync();
            var id = await PostAsync("/api/Brand", new { title = Uniq("sd-hidden"), code = "H" }, "id");
            await SendAsync(HttpMethod.Delete, $"/api/Brand/{id}", bearer: admin);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            Assert.False(await db.Brands.AnyAsync(b => b.BrandId == id));
            Assert.True(await db.Brands.IncludingInactive().AnyAsync(b => b.BrandId == id));
        }

        // ------------------------------------------------------------------------------- the join hazard

        [Fact]
        public async Task Equipment_details_still_load_when_the_catalog_rows_it_refers_to_were_deactivated_afterwards()
        {
            var status = await NewStatusAsync();
            var location = await NewDepartmentAsync();
            var type = await NewTypeAsync();
            // an inactive equipment may keep pointing at catalog rows that are deactivated later
            var equipmentId = await InsertEquipmentAsync(status, location, typeId: type, active: false);

            var admin = await AdminAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/EquipmentStatus/{status}", bearer: admin)).StatusCode);

            using var scope = _factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IEquipmentRepository>();
            var details = await repository.GetDetailsAsync(equipmentId);

            Assert.NotNull(details);   // without IncludingInactive() the join on the deactivated status would drop it
            Assert.Equal(status, details!.EquipmentStatus.EquipmentStatusId);
        }

        // ------------------------------------------------------------------------------- dependents

        [Fact]
        public async Task A_status_used_by_active_equipment_cannot_be_deactivated_until_the_equipment_is()
        {
            var status = await NewStatusAsync();
            var equipment = await InsertEquipmentAsync(status, await NewDepartmentAsync(), typeId: await NewTypeAsync());

            var admin = await AdminAsync();
            await AssertConflictAsync($"/api/EquipmentStatus/{status}");
            await SetEquipmentActiveAsync(equipment, false);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/EquipmentStatus/{status}", bearer: admin)).StatusCode);
        }

        [Fact]
        public async Task A_model_used_by_active_equipment_cannot_be_deactivated()
        {
            var model = await NewModelAsync();
            var equipment = await InsertEquipmentAsync(await NewStatusAsync(), await NewDepartmentAsync(), modelId: model);

            await AssertConflictAsync($"/api/EquipmentModel/{model}");
            await SetEquipmentActiveAsync(equipment, false);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/EquipmentModel/{model}", bearer: await AdminAsync())).StatusCode);
        }

        [Fact]
        public async Task A_type_used_directly_by_active_equipment_cannot_be_deactivated()
        {
            var type = await NewTypeAsync();
            var equipment = await InsertEquipmentAsync(await NewStatusAsync(), await NewDepartmentAsync(), typeId: type);

            await AssertConflictAsync($"/api/EquipmentType/{type}");
            await SetEquipmentActiveAsync(equipment, false);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/EquipmentType/{type}", bearer: await AdminAsync())).StatusCode);
        }

        [Fact]
        public async Task A_department_used_by_an_active_user_or_active_equipment_cannot_be_deactivated()
        {
            var admin = await AdminAsync();

            // by a user
            var byUser = await NewDepartmentAsync();
            var userName = "sd_u" + Guid.NewGuid().ToString("N")[..8];
            var create = await SendAsync(HttpMethod.Post, "/api/Users", new
            {
                userName, password = "Passw0rd!x", firstName = "A", lastName = "B", personnelCode = "PC-" + userName, supportingDepartmentIds = new[] { byUser }
            }, bearer: admin);
            Assert.True(create.IsSuccessStatusCode, await create.Content.ReadAsStringAsync());
            await AssertConflictAsync($"/api/SupportingDepartment/{byUser}");

            // by equipment
            var byEquipment = await NewDepartmentAsync();
            var equipment = await InsertEquipmentAsync(await NewStatusAsync(), byEquipment, typeId: await NewTypeAsync());
            await AssertConflictAsync($"/api/SupportingDepartment/{byEquipment}");
            await SetEquipmentActiveAsync(equipment, false);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{byEquipment}", bearer: admin)).StatusCode);
        }

        private async Task AssertConflictAsync(string deleteUrl)
        {
            var response = await SendAsync(HttpMethod.Delete, deleteUrl, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal("has_active_dependents", ErrorCodeOf(await ReadJsonAsync(response)));
        }
    }
}
