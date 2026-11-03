using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Province → City → SupportingDepartment: titles are unique only within the parent, the names of the parents travel with
    /// the child, and a parent cannot be deactivated (or reused) while that would leave an active child without an active parent.
    /// </summary>
    public class GeographyCatalogTests : ApiTestBase
    {
        public GeographyCatalogTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private async Task<JsonElement> CreateOkAsync(string url, object body)
        {
            var response = await SendAdminAsync(HttpMethod.Post, url, body);
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"POST {url} -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            return await ReadDataAsync(response);
        }

        private Task<JsonElement> NewProvinceAsync(string? title = null) => CreateOkAsync("/api/Province", new { title = title ?? Uniq("prov") });
        private Task<JsonElement> NewCityAsync(long provinceId, string? title = null) => CreateOkAsync("/api/City", new { provinceId, title = title ?? Uniq("city") });
        private Task<JsonElement> NewDepartmentAsync(long cityId, string? title = null) => CreateOkAsync("/api/SupportingDepartment", new { cityId, title = title ?? Uniq("dept") });

        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        private async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal(code, ErrorCodeOf(await ReadJsonAsync(response)));
        }

        // ---------------------------------------------------------------- title uniqueness is per parent

        [Fact]
        public async Task A_province_title_is_unique_across_provinces()
        {
            var title = Uniq("prov");
            await NewProvinceAsync(title);
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/Province", new { title }), HttpStatusCode.Conflict, "duplicate_title");
        }

        [Fact]
        public async Task A_city_title_is_unique_only_within_its_province()
        {
            var first = Id(await NewProvinceAsync());
            var second = Id(await NewProvinceAsync());
            var title = Uniq("city");

            await NewCityAsync(first, title);
            await NewCityAsync(second, title);                                  // same title, other province: allowed
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/City", new { provinceId = first, title = title.ToUpperInvariant() }),
                HttpStatusCode.Conflict, "duplicate_title");                    // same province, any casing: rejected
        }

        [Fact]
        public async Task A_department_title_is_unique_only_within_its_city()
        {
            var province = Id(await NewProvinceAsync());
            var cityA = Id(await NewCityAsync(province));
            var cityB = Id(await NewCityAsync(province));
            var title = Uniq("dept");

            await NewDepartmentAsync(cityA, title);
            await NewDepartmentAsync(cityB, title);                             // same title, other city: allowed
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/SupportingDepartment", new { cityId = cityA, title }),
                HttpStatusCode.Conflict, "duplicate_title");
        }

        [Fact]
        public async Task Moving_a_department_to_a_city_that_already_has_its_title_is_a_conflict_and_moving_to_a_free_city_works()
        {
            var province = Id(await NewProvinceAsync());
            var cityA = Id(await NewCityAsync(province));
            var cityB = Id(await NewCityAsync(province));
            var cityC = Id(await NewCityAsync(province));
            var title = Uniq("dept");

            var inA = Id(await NewDepartmentAsync(cityA, title));
            await NewDepartmentAsync(cityB, title);

            var clash = await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{inA}", new { cityId = cityB, isActive = true });
            await AssertErrorAsync(clash, HttpStatusCode.Conflict, "duplicate_title");

            var moved = await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{inA}", new { cityId = cityC, isActive = true });
            Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
            Assert.Equal(cityC, (await ReadDataAsync(moved)).GetProperty("cityId").GetInt64());
        }

        [Fact]
        public async Task Moving_a_city_to_a_province_that_already_has_its_title_is_a_conflict()
        {
            var first = Id(await NewProvinceAsync());
            var second = Id(await NewProvinceAsync());
            var title = Uniq("city");
            var city = Id(await NewCityAsync(first, title));
            await NewCityAsync(second, title);

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/City/{city}", new { provinceId = second, isActive = true }),
                HttpStatusCode.Conflict, "duplicate_title");
        }

        // ---------------------------------------------------------------- names travel with the child

        [Fact]
        public async Task A_city_carries_its_province_title_in_the_detail_and_the_list()
        {
            var provinceTitle = Uniq("prov");
            var province = Id(await NewProvinceAsync(provinceTitle));
            var created = await NewCityAsync(province);

            Assert.Equal(provinceTitle, created.GetProperty("provinceTitle").GetString());
            Assert.Equal(province, created.GetProperty("provinceId").GetInt64());

            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/City/{Id(created)}"));
            Assert.Equal(provinceTitle, detail.GetProperty("provinceTitle").GetString());

            var list = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/City?provinceId={province}")));
            Assert.Equal(provinceTitle, Assert.Single(list.EnumerateArray()).GetProperty("provinceTitle").GetString());
        }

        [Fact]
        public async Task A_department_carries_its_city_and_province_in_the_detail_and_the_list_and_filters_by_both()
        {
            var provinceTitle = Uniq("prov");
            var province = Id(await NewProvinceAsync(provinceTitle));
            var cityTitle = Uniq("city");
            var city = Id(await NewCityAsync(province, cityTitle));
            var otherCity = Id(await NewCityAsync(province));
            var otherProvinceCity = Id(await NewCityAsync(Id(await NewProvinceAsync())));

            var created = await NewDepartmentAsync(city);
            await NewDepartmentAsync(otherCity);
            await NewDepartmentAsync(otherProvinceCity);

            foreach (var json in new[]
            {
                created,
                await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment/{Id(created)}")),
            })
            {
                Assert.Equal(city, json.GetProperty("cityId").GetInt64());
                Assert.Equal(cityTitle, json.GetProperty("cityTitle").GetString());
                Assert.Equal(province, json.GetProperty("provinceId").GetInt64());
                Assert.Equal(provinceTitle, json.GetProperty("provinceTitle").GetString());
            }

            var byCity = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment?cityId={city}"))).EnumerateArray().ToList();
            Assert.Equal(Id(created), Id(Assert.Single(byCity)));
            Assert.Equal(cityTitle, byCity[0].GetProperty("cityTitle").GetString());
            Assert.Equal(provinceTitle, byCity[0].GetProperty("provinceTitle").GetString());

            // the province filter spans all of its cities, and only them
            var byProvince = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment?provinceId={province}&pageSize=100"))).EnumerateArray().ToList();
            Assert.Equal(2, byProvince.Count);
            Assert.All(byProvince, d => Assert.Equal(province, d.GetProperty("provinceId").GetInt64()));
        }

        [Fact]
        public async Task The_department_detail_shows_the_names_even_when_the_city_is_inactive()
        {
            var province = Id(await NewProvinceAsync());
            var city = Id(await NewCityAsync(province));
            var department = Id(await NewDepartmentAsync(city));

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{city}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Province/{province}")).StatusCode);

            var detail = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment/{department}"));
            Assert.False(string.IsNullOrEmpty(detail.GetProperty("cityTitle").GetString()));
            Assert.False(string.IsNullOrEmpty(detail.GetProperty("provinceTitle").GetString()));

            var inactiveList = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/SupportingDepartment?cityId={city}&isActive=false")));
            Assert.Equal(department, Id(Assert.Single(inactiveList.EnumerateArray())));
        }

        // ---------------------------------------------------------------- the hierarchy stays consistent

        [Fact]
        public async Task A_province_with_an_active_city_and_a_city_with_an_active_department_cannot_be_deactivated()
        {
            var province = Id(await NewProvinceAsync());
            var city = Id(await NewCityAsync(province));
            var department = Id(await NewDepartmentAsync(city));

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Delete, $"/api/Province/{province}"), HttpStatusCode.Conflict, "has_active_dependents");
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Delete, $"/api/City/{city}"), HttpStatusCode.Conflict, "has_active_dependents");

            // leaf first, then upwards
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{city}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Province/{province}")).StatusCode);
        }

        [Fact]
        public async Task A_city_cannot_be_created_under_an_inactive_or_missing_province()
        {
            var province = Id(await NewProvinceAsync());
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Province/{province}")).StatusCode);

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/City", new { provinceId = province, title = Uniq("city") }), HttpStatusCode.BadRequest, "parent_inactive");
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/City", new { provinceId = 987654321, title = Uniq("city") }), HttpStatusCode.NotFound, "parent_not_found");
        }

        [Fact]
        public async Task A_department_cannot_be_created_under_an_inactive_or_missing_city()
        {
            var city = Id(await NewCityAsync(Id(await NewProvinceAsync())));
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{city}")).StatusCode);

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/SupportingDepartment", new { cityId = city, title = Uniq("dept") }), HttpStatusCode.BadRequest, "parent_inactive");
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Post, "/api/SupportingDepartment", new { cityId = 987654321, title = Uniq("dept") }), HttpStatusCode.NotFound, "parent_not_found");
        }

        [Fact]
        public async Task An_inactive_city_cannot_be_reactivated_while_its_province_is_inactive_and_can_be_once_it_is_active_again()
        {
            var province = Id(await NewProvinceAsync());
            var city = Id(await NewCityAsync(province));
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{city}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Province/{province}")).StatusCode);

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/City/{city}", new { isActive = true }), HttpStatusCode.BadRequest, "parent_inactive");

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/Province/{province}", new { isActive = true })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Put, $"/api/City/{city}", new { isActive = true })).StatusCode);
        }

        [Fact]
        public async Task A_department_cannot_be_reactivated_or_moved_into_an_inactive_city()
        {
            var province = Id(await NewProvinceAsync());
            var city = Id(await NewCityAsync(province));
            var inactiveCity = Id(await NewCityAsync(province));
            var department = Id(await NewDepartmentAsync(city));
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{inactiveCity}")).StatusCode);

            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{department}", new { cityId = inactiveCity, isActive = true }),
                HttpStatusCode.BadRequest, "parent_inactive");

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/SupportingDepartment/{department}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{city}")).StatusCode);
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/SupportingDepartment/{department}", new { isActive = true }),
                HttpStatusCode.BadRequest, "parent_inactive");
        }

        [Fact]
        public async Task A_deactivated_city_title_can_be_reused_in_the_same_province()
        {
            var province = Id(await NewProvinceAsync());
            var title = Uniq("city");
            var old = Id(await NewCityAsync(province, title));
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/City/{old}")).StatusCode);

            await NewCityAsync(province, title);
            await AssertErrorAsync(await SendAdminAsync(HttpMethod.Put, $"/api/City/{old}", new { isActive = true }), HttpStatusCode.Conflict, "duplicate_title");
        }
    
        // ---------------------------------------------------------------- seeder

        [Fact]
        public async Task The_geography_seeder_adds_the_provinces_and_capitals_once_and_never_touches_existing_rows()
        {
            // a province a user already created and then deactivated: the seeder must leave it alone
            var existing = await NewProvinceAsync("تهران");
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/Province/{Id(existing)}")).StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<IGeographySeeder>();
                var first = await seeder.SeedAsync();
                Assert.Equal(30, first.Provinces);          // all but the one that already exists
                Assert.Equal(31, first.Cities);             // one capital per province, including the existing one
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<IGeographySeeder>();
                Assert.Equal((0, 0), await seeder.SeedAsync());   // second run: nothing to add

                var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
                var provinces = await db.Provinces.IncludingInactive().Where(p => p.Title == "تهران").ToListAsync();
                var tehran = Assert.Single(provinces);
                Assert.False(tehran.IsActive);              // untouched
                Assert.Equal(Id(existing), tehran.ProvinceId);

                // each seeded province exists exactly once, with its capital under it
                var fars = Assert.Single(await db.Provinces.Include(p => p.Cities).Where(p => p.Title == "فارس").ToListAsync());
                Assert.Equal("شیراز", Assert.Single(fars.Cities).Title);
            }
        }
}
}
