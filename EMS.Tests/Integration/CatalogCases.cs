using EMS.Application.Dtos.Brand;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EMS.Tests.Integration
{
    /// <summary>Describes one catalog endpoint so the same behaviour tests can run against every catalog.</summary>
    public sealed record CatalogCase(
        string Name,
        string Route,
        string IdProperty,
        string PermissionGroup,
        bool HasCode,
        Type DetailDto,
        Type ListItemDto,
        Func<ApiTestBase, string, Task<Dictionary<string, object?>>> CreateBody)
    {
        public override string ToString() => Name;

        public static readonly IReadOnlyList<CatalogCase> All = new List<CatalogCase>
        {
            new("Brand", "/api/Brand", "brandId", "Brands", true, typeof(BrandDetailDto), typeof(BrandListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "BR" })),

            new("EquipmentCategory", "/api/EquipmentCategory", "equipmentCategoryId", "EquipmentCategories", false, typeof(EquipmentCategoryDetailDto), typeof(EquipmentCategoryListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title })),

            new("EquipmentLocationType", "/api/EquipmentLocationType", "equipmentLocationTypeId", "EquipmentLocationTypes", true, typeof(EquipmentLocationTypeDetailDto), typeof(EquipmentLocationTypeListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "LT" })),

            new("EquipmentStatus", "/api/EquipmentStatus", "equipmentStatusId", "EquipmentStatuses", true, typeof(EquipmentStatusDetailDto), typeof(EquipmentStatusListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "ST" })),

            new("FailureCategory", "/api/FailureCategory", "failureCategoryId", "FailureCategories", false, typeof(FailureCategoryDetailDto), typeof(FailureCategoryListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title })),

            new("EquipmentType", "/api/EquipmentType", "equipmentTypeId", "EquipmentTypes", true, typeof(EquipmentTypeDetailDto), typeof(EquipmentTypeListItemDto),
                async (api, title) => new Dictionary<string, object?>
                {
                    ["equipmentCategoryId"] = await CatalogFixtures.NewCategoryAsync(api), ["title"] = title, ["code"] = "TY"
                }),

            new("EquipmentModel", "/api/EquipmentModel", "equipmentModelId", "EquipmentModels", true, typeof(EquipmentModelDetailDto), typeof(EquipmentModelListItemDto),
                async (api, title) => new Dictionary<string, object?>
                {
                    ["equipmentTypeId"] = await CatalogFixtures.NewTypeAsync(api),
                    ["brandId"] = await CatalogFixtures.NewBrandAsync(api),
                    ["title"] = title, ["code"] = "MO"
                }),

            new("SupportingDepartment", "/api/SupportingDepartment", "supportingDepartmentId", "SupportingDepartments", false, typeof(SupportingDepartmentDetailDto), typeof(SupportingDepartmentListItemDto),
                async (api, title) => new Dictionary<string, object?> { ["cityId"] = await CatalogFixtures.EnsureCityAsync(api), ["title"] = title }),
        };

        public static CatalogCase Get(string name) => All.Single(c => c.Name == name);
        public static IEnumerable<object[]> Names => All.Select(c => new object[] { c.Name });
    }

    /// <summary>Helpers that create the parent records some catalogs need.</summary>
    public static class CatalogFixtures
    {
        private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

        public static async Task<long> PostAsync(ApiTestBase api, string url, object body, string idProperty)
        {
            var token = (await api.LoginAdminForFixturesAsync()).AccessToken;
            var response = await api.SendForFixturesAsync(HttpMethod.Post, url, body, token);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.Created, $"POST {url} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return (await api.ReadForFixturesAsync(response)).GetProperty(idProperty).GetInt64();
        }

        public static Task<long> NewCategoryAsync(ApiTestBase api) =>
            PostAsync(api, "/api/EquipmentCategory", new { title = Unique("fx-cat") }, "equipmentCategoryId");

        public static async Task<long> NewTypeAsync(ApiTestBase api) =>
            await PostAsync(api, "/api/EquipmentType", new { equipmentCategoryId = await NewCategoryAsync(api), title = Unique("fx-type"), code = "T" }, "equipmentTypeId");

        public static Task<long> NewBrandAsync(ApiTestBase api) =>
            PostAsync(api, "/api/Brand", new { title = Unique("fx-brand"), code = "B" }, "brandId");

        public static async Task<long> EnsureCityAsync(ApiTestBase api)
        {
            using var scope = api.ServicesForFixtures.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var city = new City { Title = Unique("fx-city"), Province = new Province { Title = Unique("fx-prov") } };
            db.Cities.Add(city);
            await db.SaveChangesAsync();
            return city.CityId;
        }
    }
}
