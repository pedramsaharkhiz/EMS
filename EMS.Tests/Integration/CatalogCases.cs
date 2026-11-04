using EMS.Application.Dtos.Brand;
using EMS.Application.Dtos.City;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Dtos.Post;
using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Dtos.ActionEntity;
using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Dtos.FailureReportStatus;
using EMS.Application.Dtos.FailureType;
using EMS.Application.Dtos.ReportType;
using EMS.Application.Dtos.Province;
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
        Func<ApiTestBase, string, Task<Dictionary<string, object?>>> CreateBody,
        string? ScopeKey = null)
    {
        public override string ToString() => Name;

        public static readonly IReadOnlyList<CatalogCase> All = new List<CatalogCase>
        {
            new("Brand", "/api/Brand", "id", "Brands", true, typeof(BrandDetailDto), typeof(BrandListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "BR" })),

            new("EquipmentCategory", "/api/EquipmentCategory", "id", "EquipmentCategories", false, typeof(EquipmentCategoryDetailDto), typeof(EquipmentCategoryListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title })),

            new("EquipmentLocationType", "/api/EquipmentLocationType", "id", "EquipmentLocationTypes", true, typeof(EquipmentLocationTypeDetailDto), typeof(EquipmentLocationTypeListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "LT" })),

            new("Post", "/api/Post", "id", "Posts", true, typeof(PostDetailDto), typeof(PostListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "PO" })),

            new("EquipmentStatus", "/api/EquipmentStatus", "id", "EquipmentStatuses", true, typeof(EquipmentStatusDetailDto), typeof(EquipmentStatusListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "ST" })),

            new("FailureCategory", "/api/FailureCategory", "id", "FailureCategories", false, typeof(FailureCategoryDetailDto), typeof(FailureCategoryListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title })),

            new("FailureType", "/api/FailureType", "id", "FailureTypes", true, typeof(FailureTypeDetailDto), typeof(FailureTypeListItemDto),
                async (api, title) => new Dictionary<string, object?>
                {
                    ["failureCategoryId"] = await CatalogFixtures.PostAsync(api, "/api/FailureCategory", new { title = CatalogFixtures.Unique("fx-fcat") }, "id"),
                    ["title"] = title, ["code"] = "FT"
                }, ScopeKey: "failureCategoryId"),

            new("ReportType", "/api/ReportType", "id", "ReportTypes", true, typeof(ReportTypeDetailDto), typeof(ReportTypeListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "RT" })),

            new("ActionEntity", "/api/Action", "id", "Actions", true, typeof(ActionEntityDetailDto), typeof(ActionEntityListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "AC", ["description"] = "d" })),

            new("FailureReportStatus", "/api/FailureReportStatus", "id", "FailureReportStatuses", true, typeof(FailureReportStatusDetailDto), typeof(FailureReportStatusListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "FS" })),

            new("EquipmentType", "/api/EquipmentType", "id", "EquipmentTypes", true, typeof(EquipmentTypeDetailDto), typeof(EquipmentTypeListItemDto),
                async (api, title) => new Dictionary<string, object?>
                {
                    ["equipmentCategoryId"] = await CatalogFixtures.NewCategoryAsync(api), ["title"] = title, ["code"] = "TY"
                }),

            new("EquipmentModel", "/api/EquipmentModel", "id", "EquipmentModels", true, typeof(EquipmentModelDetailDto), typeof(EquipmentModelListItemDto),
                async (api, title) => new Dictionary<string, object?>
                {
                    ["equipmentTypeId"] = await CatalogFixtures.NewTypeAsync(api),
                    ["brandId"] = await CatalogFixtures.NewBrandAsync(api),
                    ["title"] = title, ["code"] = "MO"
                }),

            new("Province", "/api/Province", "id", "Provinces", true, typeof(ProvinceDetailDto), typeof(ProvinceListItemDto),
                (_, title) => Task.FromResult(new Dictionary<string, object?> { ["title"] = title, ["code"] = "PR" })),

            new("City", "/api/City", "id", "Cities", true, typeof(CityDetailDto), typeof(CityListItemDto),
                async (api, title) => new Dictionary<string, object?>
                {
                    ["provinceId"] = await CatalogFixtures.NewProvinceAsync(api), ["title"] = title, ["code"] = "CT"
                }, ScopeKey: "provinceId"),

            new("SupportingDepartment", "/api/SupportingDepartment", "id", "SupportingDepartments", false, typeof(SupportingDepartmentDetailDto), typeof(SupportingDepartmentListItemDto),
                async (api, title) => new Dictionary<string, object?> { ["cityId"] = await CatalogFixtures.EnsureCityAsync(api), ["title"] = title },
                ScopeKey: "cityId"),
        };

        public static CatalogCase Get(string name) => All.Single(c => c.Name == name);
        public static IEnumerable<object[]> Names => All.Select(c => new object[] { c.Name });
    }

    /// <summary>Helpers that create the parent records some catalogs need.</summary>
    public static class CatalogFixtures
    {
        public static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..30];

        public static async Task<long> PostAsync(ApiTestBase api, string url, object body, string idProperty)
        {
            var token = (await api.LoginAdminForFixturesAsync()).AccessToken;
            var response = await api.SendForFixturesAsync(HttpMethod.Post, url, body, token);
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.Created, $"POST {url} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return (await api.ReadForFixturesAsync(response)).GetProperty(idProperty).GetInt64();
        }

        public static Task<long> NewCategoryAsync(ApiTestBase api) =>
            PostAsync(api, "/api/EquipmentCategory", new { title = Unique("fx-cat") }, "id");

        public static async Task<long> NewTypeAsync(ApiTestBase api) =>
            await PostAsync(api, "/api/EquipmentType", new { equipmentCategoryId = await NewCategoryAsync(api), title = Unique("fx-type"), code = "T" }, "id");

        public static Task<long> NewBrandAsync(ApiTestBase api) =>
            PostAsync(api, "/api/Brand", new { title = Unique("fx-brand"), code = "B" }, "id");

        public static Task<long> NewProvinceAsync(ApiTestBase api) =>
            PostAsync(api, "/api/Province", new { title = Unique("fx-prov") }, "id");

        public static async Task<long> NewCityAsync(ApiTestBase api, long? provinceId = null, string? title = null) =>
            await PostAsync(api, "/api/City", new { provinceId = provinceId ?? await NewProvinceAsync(api), title = title ?? Unique("fx-city") }, "id");

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
