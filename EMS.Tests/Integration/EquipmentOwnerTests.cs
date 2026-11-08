using EMS.Domain;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>The owner of an equipment must be a defined owner: the API refuses anything else in Persian, and the table has a CHECK constraint that says the same.</summary>
    public class EquipmentOwnerTests : ApiTestBase
    {
        public EquipmentOwnerTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private const string Message = "مالک تجهیز باید شرکت یا بانک باشد.";

        private sealed record Fixture(long TypeId, long DepartmentId);
        private async Task<Fixture> NewFixtureAsync()
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("owcat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("owtype"), code = "T" }, "id");
            var location = await CatalogFixtures.NewDepartmentAsync(this);
            return new Fixture(type, location);
        }

        private static Dictionary<string, object?> Body(Fixture f, object? owner) => new()
        {
            ["yaasCode"] = Uniq("OWY"), ["owner"] = owner, ["equipmentTypeId"] = f.TypeId,
            ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock, ["currentSupportingDepartmentId"] = f.DepartmentId,
        };

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        [Fact]
        public async Task A_missing_zero_or_unknown_owner_is_refused_in_persian_on_create_and_update_and_bulk()
        {
            var f = await NewFixtureAsync();
            var created = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, (int)EquipmentOwner.Bank));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var id = (await ReadDataAsync(created)).GetProperty("id").GetInt64();

            foreach (var owner in new object?[] { null, 0, 3, -1, 99 })
            {
                var create = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, owner));
                Assert.True(create.StatusCode == HttpStatusCode.BadRequest, $"create owner={owner} -> {(int)create.StatusCode}");
                var json = await ReadJsonAsync(create);
                Assert.Contains("owner", ErrorFieldsOf(json));
                // a JSON null cannot even be bound to the number: MVC's generic Persian "invalid value"; anything else gets the specific message
                var messages = json.GetProperty("error").GetProperty("details").EnumerateArray().Select(d => d.GetProperty("message").GetString()!).ToList();
                if (owner is not null) Assert.Contains(Message, messages);
                Assert.All(messages, m => Assert.Matches("[؀-ۿ]", m));

                var update = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", new Dictionary<string, object?>
                {
                    ["yaasCode"] = Uniq("OWU"), ["owner"] = owner, ["equipmentTypeId"] = f.TypeId, ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock, ["isActive"] = true,
                });
                Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
                Assert.Contains("owner", ErrorFieldsOf(await ReadJsonAsync(update)));

                var bulk = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items = new[] { Body(f, owner) } });
                Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
                Assert.Contains("items[0].owner", ErrorFieldsOf(await ReadJsonAsync(bulk)));
            }

            // a value that is not even a number is a Persian validation error too
            var text = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, "abc"));
            Assert.Equal(HttpStatusCode.BadRequest, text.StatusCode);
            Assert.Matches("[؀-ۿ]", (await ReadJsonAsync(text)).GetProperty("message").GetString()!);
        }

        [Fact]
        public async Task Both_owners_are_accepted_and_shown_with_their_titles()
        {
            var f = await NewFixtureAsync();
            foreach (var (owner, title) in new[] { (EquipmentOwner.Company, "شرکت"), (EquipmentOwner.Bank, "بانک") })
            {
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", Body(f, (int)owner));
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                Assert.Equal(title, (await ReadDataAsync(response)).GetProperty("ownerTitle").GetString());
            }
        }

        [Fact]
        public void The_table_has_a_check_constraint_for_exactly_the_defined_owners()
        {
            using var scope = _factory.Services.CreateScope();
            var model = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>(scope.ServiceProvider.GetRequiredService<EMSDbContext>()).Model;
            var equipment = model.FindEntityType(typeof(EMS.Domain.Entities.Equipment))!;
            var check = equipment.GetCheckConstraints().Single(c => c.Name == EquipmentConstraints.OwnerCheckName);
            foreach (var owner in Enum.GetValues<EquipmentOwner>())
                Assert.Contains(((int)owner).ToString(), check.Sql);
            Assert.DoesNotContain("0", check.Sql.Replace("10", ""));    // 0 (the old default) is not an owner
        }
    }
}
