using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>A catalog row with a parent shows the parent's title next to its id, in the list, the detail and the answer of a create (like a city shows its province).</summary>
    public class CatalogParentTitleTests : ApiTestBase
    {
        public CatalogParentTitleTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task AssertTitleAsync(string baseUrl, long id, string search, string property, string expected, string? createdTitle = null)
        {
            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"{baseUrl}/{id}"));
            Assert.Equal(expected, detail.GetProperty(property).GetString());

            var list = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"{baseUrl}?pageSize=100&title={Uri.EscapeDataString(search)}")));
            var row = list.EnumerateArray().Single(x => x.GetProperty("id").GetInt64() == id);
            Assert.Equal(expected, row.GetProperty(property).GetString());
        }

        [Fact]
        public async Task Equipment_type_model_and_failure_type_show_the_titles_of_their_parents()
        {
            var categoryTitle = Uniq("ptcat");
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = categoryTitle }, "id");
            var typeTitle = Uniq("pttype");
            var typeResponse = await SendAdminAsync(HttpMethod.Post, "/api/EquipmentType", new { equipmentCategoryId = category, title = typeTitle, code = "T" });
            Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
            var created = await ReadDataAsync(typeResponse);
            Assert.Equal(categoryTitle, created.GetProperty("equipmentCategoryTitle").GetString());      // the answer of the create has it too
            var typeId = created.GetProperty("id").GetInt64();
            await AssertTitleAsync("/api/EquipmentType", typeId, typeTitle, "equipmentCategoryTitle", categoryTitle);

            var brandTitle = Uniq("ptbrand");
            var brand = await CatalogFixtures.PostAsync(this, "/api/Brand", new { title = brandTitle, code = "B" }, "id");
            var modelTitle = Uniq("ptmodel");
            var model = await CatalogFixtures.PostAsync(this, "/api/EquipmentModel", new { equipmentTypeId = typeId, brandId = brand, title = modelTitle, code = "M" }, "id");
            await AssertTitleAsync("/api/EquipmentModel", model, modelTitle, "equipmentTypeTitle", typeTitle);
            await AssertTitleAsync("/api/EquipmentModel", model, modelTitle, "brandTitle", brandTitle);

            var failureCategoryTitle = Uniq("ptfcat");
            var failureCategory = await CatalogFixtures.PostAsync(this, "/api/FailureCategory", new { title = failureCategoryTitle }, "id");
            var failureTypeTitle = Uniq("ptftype");
            var failureType = await CatalogFixtures.PostAsync(this, "/api/FailureType", new { failureCategoryId = failureCategory, title = failureTypeTitle, code = "F" }, "id");
            await AssertTitleAsync("/api/FailureType", failureType, failureTypeTitle, "failureCategoryTitle", failureCategoryTitle);
        }
    }
}
