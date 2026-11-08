using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The two lookups of the equipment form: GET /api/EquipmentType/without-models (types an equipment can only be registered with directly) and
    /// GET /api/EquipmentType/by-model/{modelId} (the type, and its category, of a picked model).
    /// </summary>
    public class EquipmentTypeLookupTests : ApiTestBase
    {
        public EquipmentTypeLookupTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private sealed record Type(long Id, string Title, long CategoryId, string CategoryTitle);
        private async Task<Type> NewTypeAsync(long? categoryId = null, string? categoryTitle = null)
        {
            categoryTitle ??= Uniq("lkcat");
            categoryId ??= await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = categoryTitle }, "id");
            var title = Uniq("lktype");
            var id = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = categoryId, title, code = "T" }, "id");
            return new Type(id, title, categoryId.Value, categoryTitle);
        }

        private async Task<long> NewModelAsync(long typeId)
        {
            var brand = await CatalogFixtures.PostAsync(this, "/api/Brand", new { title = Uniq("lkbrand"), code = "B" }, "id");
            return await CatalogFixtures.PostAsync(this, "/api/EquipmentModel", new { equipmentTypeId = typeId, brandId = brand, title = Uniq("lkmodel"), code = "M" }, "id");
        }

        private async Task<List<JsonElement>> WithoutModelsAsync(string query = "")
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/without-models?pageSize=100{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return DataOf(await ReadJsonAsync(response)).EnumerateArray().ToList();
        }

        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        // ------------------------------------------------------------------------------------ without-models

        [Fact]
        public async Task A_type_without_models_is_listed_with_its_category_and_one_with_an_active_model_is_not()
        {
            var plain = await NewTypeAsync();
            var withModel = await NewTypeAsync();
            var model = await NewModelAsync(withModel.Id);

            var listed = (await WithoutModelsAsync()).ToDictionary(Id);
            Assert.True(listed.ContainsKey(plain.Id));
            Assert.False(listed.ContainsKey(withModel.Id));

            var row = listed[plain.Id];
            Assert.Equal(plain.Title, row.GetProperty("title").GetString());
            Assert.Equal(plain.CategoryId, row.GetProperty("equipmentCategoryId").GetInt64());
            Assert.Equal(plain.CategoryTitle, row.GetProperty("equipmentCategoryTitle").GetString());
            Assert.True(row.GetProperty("isActive").GetBoolean());

            // when its only model is deactivated the type is a type without models again
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentModel/{model}")).StatusCode);
            Assert.Contains(withModel.Id, (await WithoutModelsAsync()).Select(Id));
        }

        [Fact]
        public async Task A_deactivated_type_is_not_listed_and_the_list_filters_sorts_and_pages()
        {
            var categoryTitle = Uniq("lkfcat");
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = categoryTitle }, "id");
            var a = await NewTypeAsync(category, categoryTitle);
            var b = await NewTypeAsync(category, categoryTitle);
            var c = await NewTypeAsync(category, categoryTitle);
            var other = await NewTypeAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentType/{c.Id}")).StatusCode);

            var inCategory = (await WithoutModelsAsync($"&equipmentCategoryId={category}")).Select(Id).ToList();
            Assert.Equal(new[] { a.Id, b.Id }.OrderBy(i => i), inCategory.OrderBy(i => i));
            Assert.DoesNotContain(other.Id, inCategory);
            Assert.DoesNotContain(c.Id, inCategory);

            var byTitle = (await WithoutModelsAsync($"&equipmentCategoryId={category}&title={b.Title}")).Select(Id).ToList();
            Assert.Equal(new[] { b.Id }, byTitle);

            var descending = (await WithoutModelsAsync($"&equipmentCategoryId={category}&sortBy=title&sortDirection=desc")).Select(x => x.GetProperty("title").GetString()).ToList();
            Assert.Equal(descending.OrderByDescending(t => t, StringComparer.Ordinal), descending);

            var page = await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/without-models?equipmentCategoryId={category}&pageSize=1&pageNumber=2"));
            Assert.Equal(1, DataOf(page).GetArrayLength());
            Assert.Equal(2, page.GetProperty("meta").GetProperty("pagination").GetProperty("totalCount").GetInt32());
        }

        [Fact]
        public async Task The_lookup_is_open_to_any_signed_in_user_and_validates_its_filters_in_persian()
        {
            var admin = await AdminAsync();
            var userName = "lkplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/EquipmentType/without-models", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/EquipmentType/without-models")).StatusCode);

            var bad = await SendAdminAsync(HttpMethod.Get, "/api/EquipmentType/without-models?pageSize=0&equipmentCategoryId=0&title=" + new string('x', 101));
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            var json = await ReadJsonAsync(bad);
            Assert.Equal("validation_failed", ErrorCodeOf(json));
            foreach (var d in json.GetProperty("error").GetProperty("details").EnumerateArray())
                Assert.Matches("[؀-ۿ]", d.GetProperty("message").GetString()!);
        }

        // ------------------------------------------------------------------------------------ hasModels

        [Fact]
        public async Task The_type_list_says_for_each_type_whether_it_has_an_active_model()
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("hmcat") }, "id");
            var plain = await NewTypeAsync(category);
            var withModel = await NewTypeAsync(category);
            var withOnlyInactiveModel = await NewTypeAsync(category);
            await NewModelAsync(withModel.Id);
            var inactiveModel = await NewModelAsync(withOnlyInactiveModel.Id);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentModel/{inactiveModel}")).StatusCode);

            async Task<Dictionary<long, bool>> HasModelsAsync(string query)
            {
                var json = await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType?pageSize=100&equipmentCategoryId={category}{query}"));
                return DataOf(json).EnumerateArray().ToDictionary(t => t.GetProperty("id").GetInt64(), t => t.GetProperty("hasModels").GetBoolean());
            }

            var list = await HasModelsAsync("");
            Assert.False(list[plain.Id]);                    // no model at all
            Assert.True(list[withModel.Id]);                 // an active model
            Assert.False(list[withOnlyInactiveModel.Id]);    // its only model is deactivated: same meaning as without-models

            // the list of deactivated types (the soft-delete filter is lifted there) says the same
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentType/{plain.Id}")).StatusCode);
            Assert.False((await HasModelsAsync("&isActive=false"))[plain.Id]);

            // the two other endpoints that return the same item shape agree with it
            var withoutModels = await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/without-models?pageSize=100&equipmentCategoryId={category}"));
            Assert.All(DataOf(withoutModels).EnumerateArray(), t => Assert.False(t.GetProperty("hasModels").GetBoolean()));
            Assert.Contains(DataOf(withoutModels).EnumerateArray(), t => Id(t) == withOnlyInactiveModel.Id);

            var byModel = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{await NewModelAsync(withModel.Id)}"));
            Assert.True(byModel.GetProperty("hasModels").GetBoolean());
            var byInactiveModel = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{inactiveModel}"));
            Assert.False(byInactiveModel.GetProperty("hasModels").GetBoolean());                  // the model asked about is deactivated and is its type's only one
        }

        // ------------------------------------------------------------------------------------ by-model

        [Fact]
        public async Task By_model_returns_the_type_of_the_model_with_its_category()
        {
            var type = await NewTypeAsync();
            var model = await NewModelAsync(type.Id);

            var response = await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{model}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);
            Assert.Equal(type.Id, Id(json));
            Assert.Equal(type.Title, json.GetProperty("title").GetString());
            Assert.Equal(type.CategoryId, json.GetProperty("equipmentCategoryId").GetInt64());
            Assert.Equal(type.CategoryTitle, json.GetProperty("equipmentCategoryTitle").GetString());
            Assert.True(json.GetProperty("isActive").GetBoolean());
        }

        [Fact]
        public async Task By_model_still_answers_for_a_deactivated_model_or_type_and_says_so()
        {
            var type = await NewTypeAsync();
            var model = await NewModelAsync(type.Id);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentModel/{model}")).StatusCode);
            Assert.Equal(type.Id, Id(await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{model}"))));

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/EquipmentType/{type.Id}")).StatusCode);
            var json = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{model}"));
            Assert.False(json.GetProperty("isActive").GetBoolean());
            Assert.Equal(type.Title, json.GetProperty("title").GetString());
        }

        [Fact]
        public async Task By_model_refuses_an_unknown_or_invalid_model_in_persian_and_is_open_to_any_signed_in_user()
        {
            var unknown = await SendAdminAsync(HttpMethod.Get, "/api/EquipmentType/by-model/99999999");
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            var json = await ReadJsonAsync(unknown);
            Assert.Equal("not_found", ErrorCodeOf(json));
            Assert.Matches("[؀-ۿ]", json.GetProperty("message").GetString()!);

            var zero = await SendAdminAsync(HttpMethod.Get, "/api/EquipmentType/by-model/0");
            Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
            Assert.Contains("modelId", ErrorFieldsOf(await ReadJsonAsync(zero)));

            var type = await NewTypeAsync();
            var model = await NewModelAsync(type.Id);
            var admin = await AdminAsync();
            var userName = "lkplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{model}", bearer: plain)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/EquipmentType/by-model/{model}")).StatusCode);
        }
    }
}
