using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>Soft-delete and concurrency behaviour of the catalog endpoints (brands, categories, types, models, ...).</summary>
    public class CatalogIntegrityTests : ApiTestBase
    {
        public CatalogIntegrityTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;

        private async Task<long> CreateAsync(string url, object body, string idProperty)
        {
            var response = await SendAsync(HttpMethod.Post, url, body, bearer: await AdminAsync());
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"POST {url} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return (await ReadDataAsync(response)).GetProperty(idProperty).GetInt64();
        }

        private async Task<HttpResponseMessage> PutAsync(string url, object body) =>
            await SendAsync(HttpMethod.Put, url, body, bearer: await AdminAsync());

        private async Task<HttpResponseMessage> DeleteAsync(string url) =>
            await SendAsync(HttpMethod.Delete, url, bearer: await AdminAsync());

        private async Task<HttpResponseMessage> GetAsync(string url) =>
            await SendAsync(HttpMethod.Get, url, bearer: await AdminAsync());

        private static IEnumerable<string> Titles(JsonElement page) =>
            DataOf(page).EnumerateArray().Select(i => i.GetProperty("title").GetString()!);

        // ---------------------------------------------------------------- title reuse / listing

        [Fact]
        public async Task A_deactivated_records_title_can_be_reused_and_the_old_record_can_still_be_found_and_restored()
        {
            var id = await CreateAsync("/api/Brand", new { title = "CatReuse", code = "R1" }, "id");
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/Brand/{id}")).StatusCode);

            // Before the fix the title stayed reserved forever (409). Now it can be used again.
            var secondId = await CreateAsync("/api/Brand", new { title = "CatReuse", code = "R2" }, "id");
            Assert.NotEqual(id, secondId);

            // Default listing: active records only. ?isActive=false: the deactivated ones.
            var active = await ReadJsonAsync(await GetAsync("/api/Brand?pageSize=100"));
            var inactive = await ReadJsonAsync(await GetAsync("/api/Brand?pageSize=100&isActive=false"));
            Assert.Contains("CatReuse", Titles(active));
            Assert.Equal(1, Titles(inactive).Count(t => t == "CatReuse"));
            Assert.DoesNotContain(DataOf(inactive).EnumerateArray(), i => i.GetProperty("isActive").GetBoolean());

            // The deactivated record is still readable by id...
            Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Brand/{id}")).StatusCode);

            // ...but cannot be restored while its title is taken by an active record.
            var restoreClash = await PutAsync($"/api/Brand/{id}", new { title = "CatReuse", code = "R1", isActive = true });
            Assert.Equal(HttpStatusCode.Conflict, restoreClash.StatusCode);

            // Free the title, then restoring works.
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/Brand/{secondId}")).StatusCode);
            var restore = await PutAsync($"/api/Brand/{id}", new { title = "CatReuse", code = "R1", isActive = true });
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
            Assert.True((await ReadDataAsync(restore)).GetProperty("isActive").GetBoolean());
        }

        // ---------------------------------------------------------------- dependents

        [Fact]
        public async Task A_record_with_active_dependents_cannot_be_deactivated_until_they_are()
        {
            var categoryId = await CreateAsync("/api/EquipmentCategory", new { title = "DepCat" }, "id");
            var typeId = await CreateAsync("/api/EquipmentType", new { equipmentCategoryId = categoryId, title = "DepType", code = "DT" }, "id");
            var brandId = await CreateAsync("/api/Brand", new { title = "DepBrand", code = "DB" }, "id");
            var modelId = await CreateAsync("/api/EquipmentModel", new { equipmentTypeId = typeId, brandId, title = "DepModel", code = "DM" }, "id");

            // category <- type <- model, brand <- model
            Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync($"/api/EquipmentCategory/{categoryId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync($"/api/EquipmentType/{typeId}")).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await DeleteAsync($"/api/Brand/{brandId}")).StatusCode);

            // The same rule applies when deactivating through an update request.
            var viaUpdate = await PutAsync($"/api/Brand/{brandId}", new { title = "DepBrand", code = "DB", isActive = false });
            Assert.Equal(HttpStatusCode.Conflict, viaUpdate.StatusCode);
            Assert.True((await ReadDataAsync(await GetAsync($"/api/Brand/{brandId}"))).GetProperty("isActive").GetBoolean());

            // Bottom-up works.
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/EquipmentModel/{modelId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/Brand/{brandId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/EquipmentType/{typeId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/EquipmentCategory/{categoryId}")).StatusCode);
        }

        [Fact]
        public async Task New_and_restored_records_cannot_hang_below_an_inactive_parent()
        {
            var categoryId = await CreateAsync("/api/EquipmentCategory", new { title = "ParCat" }, "id");
            var typeId = await CreateAsync("/api/EquipmentType", new { equipmentCategoryId = categoryId, title = "ParType", code = "PT" }, "id");
            var brandId = await CreateAsync("/api/Brand", new { title = "ParBrand", code = "PB" }, "id");
            var modelId = await CreateAsync("/api/EquipmentModel", new { equipmentTypeId = typeId, brandId, title = "ParModel", code = "PM" }, "id");

            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/EquipmentModel/{modelId}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await DeleteAsync($"/api/Brand/{brandId}")).StatusCode);

            // creating a model under the now-inactive brand is rejected
            var create = await SendAsync(HttpMethod.Post, "/api/EquipmentModel",
                new { equipmentTypeId = typeId, brandId, title = "ParModel2", code = "P2" }, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);

            // restoring the model while its brand is inactive is rejected too
            var restore = await PutAsync($"/api/EquipmentModel/{modelId}", new { title = "ParModel", code = "PM", isActive = true });
            Assert.Equal(HttpStatusCode.BadRequest, restore.StatusCode);

            // after the brand is restored, the model can be restored
            Assert.Equal(HttpStatusCode.OK, (await PutAsync($"/api/Brand/{brandId}", new { title = "ParBrand", code = "PB", isActive = true })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await PutAsync($"/api/EquipmentModel/{modelId}", new { title = "ParModel", code = "PM", isActive = true })).StatusCode);
        }

        // ---------------------------------------------------------------- optimistic concurrency

        [Fact]
        public async Task Updating_with_a_stale_row_version_is_rejected_with_409_instead_of_overwriting_the_other_change()
        {
            var id = await CreateAsync("/api/Brand", new { title = "ConBrand", code = "C0" }, "id");
            var v0 = (await ReadDataAsync(await GetAsync($"/api/Brand/{id}"))).GetProperty("rowVersion").GetString()!;

            // User A saves with the version they read.
            var a = await PutAsync($"/api/Brand/{id}", new { title = "ConBrand", code = "C1", isActive = true, rowVersion = v0 });
            Assert.Equal(HttpStatusCode.OK, a.StatusCode);
            var v1 = (await ReadDataAsync(a)).GetProperty("rowVersion").GetString()!;
            Assert.NotEqual(v0, v1);

            // User B still holds the old version: rejected, A's change survives.
            var b = await PutAsync($"/api/Brand/{id}", new { title = "ConBrand", code = "C-B", isActive = true, rowVersion = v0 });
            Assert.Equal(HttpStatusCode.Conflict, b.StatusCode);
            Assert.Equal("C1", (await ReadDataAsync(await GetAsync($"/api/Brand/{id}"))).GetProperty("code").GetString());

            // B reloads and saves with the current version.
            Assert.Equal(HttpStatusCode.OK, (await PutAsync($"/api/Brand/{id}", new { title = "ConBrand", code = "C2", isActive = true, rowVersion = v1 })).StatusCode);

            // Existing clients that do not send a version keep working (last write wins).
            Assert.Equal(HttpStatusCode.OK, (await PutAsync($"/api/Brand/{id}", new { title = "ConBrand", code = "C3", isActive = true })).StatusCode);

            // A malformed version is a client error, not a server error.
            Assert.Equal(HttpStatusCode.BadRequest, (await PutAsync($"/api/Brand/{id}", new { title = "ConBrand", code = "C4", isActive = true, rowVersion = "not-a-number" })).StatusCode);
        }
    }
}
