using EMS.Domain.Enums;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>GET /api/Users/{userId}/equipment: the equipment a user holds now (he is its technician), as the equipment list.</summary>
    public class UserEquipmentTests : ApiTestBase
    {
        public UserEquipmentTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<long> NewUserAsync() =>
            await CreateUserAsync(await AdminAsync(), "ue_" + Guid.NewGuid().ToString("N")[..10], "Passw0rd!x");

        private async Task<long> NewEquipmentAsync(long? technician, long? status = null)
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("uecat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("uetype"), code = "T" }, "id");
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", new Dictionary<string, object?>
            {
                ["yaasCode"] = Uniq("UE"), ["owner"] = 1, ["equipmentTypeId"] = type, ["equipmentStatusId"] = status ?? (long)EquipmentStatusCode.InStock,
                ["currentSupportingDepartmentId"] = await CatalogFixtures.NewDepartmentAsync(this), ["technicianUserId"] = technician,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        private async Task<List<long>> IdsAsync(long userId, string query = "")
        {
            var response = await SendAdminAsync(HttpMethod.Get, $"/api/Users/{userId}/equipment?pageSize=100{query}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return DataOf(await ReadJsonAsync(response)).EnumerateArray().Select(e => e.GetProperty("id").GetInt64()).ToList();
        }

        [Fact]
        public async Task It_lists_only_the_equipment_the_user_holds_in_the_shape_of_the_equipment_list()
        {
            var user = await NewUserAsync();
            var other = await NewUserAsync();
            var a = await NewEquipmentAsync(user);
            var b = await NewEquipmentAsync(user);
            await NewEquipmentAsync(other);
            await NewEquipmentAsync(null);

            Assert.Equal(new[] { a, b }.OrderBy(x => x), (await IdsAsync(user)).OrderBy(x => x));

            var first = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Users/{user}/equipment"))).EnumerateArray().First();
            Assert.Equal(user, first.GetProperty("technicianUserId").GetInt64());
            foreach (var name in new[] { "yaasCode", "effectiveTypeTitle", "equipmentStatusTitle", "locationKind", "attachmentCount", "technicianName" })
                Assert.True(first.TryGetProperty(name, out _), name);
        }

        [Fact]
        public async Task The_filters_the_sorting_and_the_paging_of_the_list_work_and_the_user_of_the_route_decides()
        {
            var user = await NewUserAsync();
            var other = await NewUserAsync();
            var inStock = await NewEquipmentAsync(user);
            var inUse = await NewEquipmentAsync(user, (long)EquipmentStatusCode.InUse);
            var deactivated = await NewEquipmentAsync(user);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Equipment/{deactivated}")).StatusCode);

            // a status filter, as in the list; the deactivated one is only in the list of deactivated equipment
            Assert.Equal(new[] { inUse }, await IdsAsync(user, $"&statusId={(long)EquipmentStatusCode.InUse}"));
            Assert.Equal(new[] { deactivated }, await IdsAsync(user, "&isActive=false"));
            Assert.Equal(new[] { inUse, inStock }, await IdsAsync(user, "&sortDirection=desc"));

            var page = await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/Users/{user}/equipment?pageSize=1&pageNumber=2&sortDirection=desc"));
            Assert.Equal(2, PaginationOf(page).GetProperty("totalCount").GetInt32());
            Assert.Equal(inStock, DataOf(page).EnumerateArray().Single().GetProperty("id").GetInt64());

            // a technicianUserId in the query does not change whose equipment it is
            Assert.Equal(new[] { inStock, inUse }.OrderBy(x => x), (await IdsAsync(user, $"&technicianUserId={other}")).OrderBy(x => x));
        }

        [Fact]
        public async Task A_user_who_holds_nothing_is_an_empty_list_and_an_unknown_user_is_404()
        {
            var user = await NewUserAsync();
            Assert.Empty(await IdsAsync(user));

            var unknown = await SendAdminAsync(HttpMethod.Get, "/api/Users/987654321/equipment");
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            var json = await ReadJsonAsync(unknown);
            Assert.Equal("user_not_found", ErrorCodeOf(json));
            Assert.Matches("[؀-ۿ]", json.GetProperty("message").GetString()!);

            // an invalid list parameter is a validation error, as in the list
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Get, $"/api/Users/{user}/equipment?pageSize=101")).StatusCode);
        }

        [Fact]
        public async Task A_deactivated_user_still_shows_what_he_holds_and_the_equipment_view_permission_is_needed()
        {
            var user = await NewUserAsync();
            var equipment = await NewEquipmentAsync(user);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Users/{user}")).StatusCode);
            Assert.Equal(new[] { equipment }, await IdsAsync(user));

            var admin = await AdminAsync();
            async Task<string> TokenWithAsync(string name, params string[] permissions)
            {
                var userName = name + Guid.NewGuid().ToString("N")[..8];
                var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
                var roleId = await CreateRoleAsync(admin, "UeRole_" + userName, permissions);
                await AssignRoleAsync(admin, userId, roleId);
                return (await LoginAsync(userName, "Passw0rd!x")).AccessToken;
            }

            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/Users/{user}/equipment")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, $"/api/Users/{user}/equipment", await TokenWithAsync("uen_", "Users.View")));
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, $"/api/Users/{user}/equipment", await TokenWithAsync("uev_", "Equipments.View")));
        }
    }
}
