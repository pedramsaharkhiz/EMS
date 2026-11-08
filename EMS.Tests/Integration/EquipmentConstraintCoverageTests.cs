using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// Every constraint of the equipment table has a Persian validation in the API, so a request is refused with a clear message before the
    /// database has to: lengths, NOT NULLs, the CHECK constraints, the foreign keys and the unique indexes. The tests read the constraints from
    /// the EF model, so a constraint added later without a validation and a case here fails the build (the lists below are exact).
    /// </summary>
    public class EquipmentConstraintCoverageTests : ApiTestBase
    {
        public EquipmentConstraintCoverageTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];

        private IEntityType Equipment()
        {
            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<EMSDbContext>().GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Equipment))!;
        }

        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());

        private sealed record Fixture(long TypeId, long ModelId, long DepartmentId);
        private async Task<Fixture> NewFixtureAsync()
        {
            var category = await CatalogFixtures.PostAsync(this, "/api/EquipmentCategory", new { title = Uniq("cccat") }, "id");
            var type = await CatalogFixtures.PostAsync(this, "/api/EquipmentType", new { equipmentCategoryId = category, title = Uniq("cctype"), code = "T" }, "id");
            var brand = await CatalogFixtures.PostAsync(this, "/api/Brand", new { title = Uniq("ccbrand"), code = "B" }, "id");
            var model = await CatalogFixtures.PostAsync(this, "/api/EquipmentModel", new { equipmentTypeId = type, brandId = brand, title = Uniq("ccmodel"), code = "M" }, "id");
            var location = await CatalogFixtures.NewDepartmentAsync(this);
            return new Fixture(type, model, location);
        }

        private static Dictionary<string, object?> Body(Fixture f) => new()
        {
            ["yaasCode"] = Uniq("CCY"), ["owner"] = (int)EquipmentOwner.Company, ["equipmentTypeId"] = f.TypeId,
            ["equipmentStatusId"] = (long)EquipmentStatusCode.InStock, ["currentSupportingDepartmentId"] = f.DepartmentId,
        };

        private static Dictionary<string, object?> UpdateBody(Dictionary<string, object?> create) => new()
        {
            ["yaasCode"] = create["yaasCode"], ["owner"] = create["owner"], ["equipmentTypeId"] = create["equipmentTypeId"],
            ["equipmentModelId"] = create.GetValueOrDefault("equipmentModelId"), ["equipmentStatusId"] = create["equipmentStatusId"], ["isActive"] = true,
        };

        private static void AssertPersianValidation(JsonElement json, string field)
        {
            Assert.Equal("validation_failed", ErrorCodeOf(json));
            var details = json.GetProperty("error").GetProperty("details").EnumerateArray().ToList();
            var detail = details.FirstOrDefault(d => d.GetProperty("field").GetString() == field);
            Assert.True(detail.ValueKind != JsonValueKind.Undefined, $"no error for '{field}': {json}");
            Assert.Matches("[؀-ۿ]", detail.GetProperty("message").GetString()!);
        }

        private async Task<long> CreatedIdAsync(Dictionary<string, object?> body)
        {
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        // ------------------------------------------------------------------------------------ lengths

        public static IEnumerable<object[]> Lengths => new[]
        {
            new object[] { "YaasCode", "yaasCode" }, new object[] { "SerialNumber", "serialNumber" },
            new object[] { "AssetNumber", "assetNumber" }, new object[] { "Description", "description" },
        };

        [Theory, MemberData(nameof(Lengths))]
        public async Task A_value_longer_than_its_column_is_refused_in_persian_everywhere(string property, string field)
        {
            var max = Equipment().FindProperty(property)!.GetMaxLength()!.Value;
            var f = await NewFixtureAsync();
            var longValue = new string('x', max + 1);
            var fits = new string('y', max);

            var create = Body(f); create[field] = longValue;
            var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", create);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            AssertPersianValidation(await ReadJsonAsync(response), field);

            var bulk = await SendAdminAsync(HttpMethod.Post, "/api/Equipment/bulk", new { items = new[] { create } });
            Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
            AssertPersianValidation(await ReadJsonAsync(bulk), $"items[0].{field}");

            var id = await CreatedIdAsync(Body(f));
            var update = UpdateBody(Body(f)); update[field] = longValue;
            var updated = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", update);
            Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
            AssertPersianValidation(await ReadJsonAsync(updated), field);

            // exactly the column's length is fine
            var ok = Body(f); ok[field] = fits;
            Assert.Equal(HttpStatusCode.Created, (await SendAdminAsync(HttpMethod.Post, "/api/Equipment", ok)).StatusCode);
        }

        [Fact]
        public async Task The_location_history_description_is_limited_like_its_column()
        {
            var f = await NewFixtureAsync();
            var id = await CreatedIdAsync(Body(f));
            var max = Model().FindEntityType(typeof(EquipmentLocationHistory))!.FindProperty("Description")!.GetMaxLength()!.Value;

            var move = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{id}/move", new { currentSupportingDepartmentId = f.DepartmentId, description = new string('x', max + 1) });
            Assert.Equal(HttpStatusCode.BadRequest, move.StatusCode);
            AssertPersianValidation(await ReadJsonAsync(move), "description");
        }

        private IModel Model()
        {
            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<EMSDbContext>().GetService<IDesignTimeModel>().Model;
        }

        // ------------------------------------------------------------------------------------ NOT NULL

        [Fact]
        public async Task Every_required_column_is_required_by_the_validation_with_a_persian_message()
        {
            // the NOT NULL columns of the table, minus the ones the server fills (key, state, audit); a new one must be added here AND validated
            var notNull = Equipment().GetProperties()
                .Where(p => !p.IsNullable && !p.IsPrimaryKey())
                .Select(p => p.Name)
                .Except(new[] { "IsActive", "CreatedByUserId", "CreatedDateTime" })
                .OrderBy(n => n).ToArray();
            Assert.Equal(new[] { "EquipmentStatusId", "Owner", "YaasCode" }, notNull);

            var f = await NewFixtureAsync();
            foreach (var (property, field) in new[] { ("YaasCode", "yaasCode"), ("EquipmentStatusId", "equipmentStatusId"), ("Owner", "owner") })
            {
                var body = Body(f);
                body.Remove(field);
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
                Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"{property} -> {(int)response.StatusCode}");
                var json = await ReadJsonAsync(response);
                Assert.Contains(field, ErrorFieldsOf(json));
                Assert.Matches("[؀-ۿ]", json.GetProperty("message").GetString()!);
            }
        }

        // ------------------------------------------------------------------------------------ CHECK constraints

        [Fact]
        public async Task Every_check_constraint_has_a_validation_case()
        {
            var names = Equipment().GetCheckConstraints().Select(c => c.Name).OrderBy(n => n).ToArray();
            Assert.Equal(new[] { "CK_Equipments_Location_ExactlyOne", "CK_Equipments_Owner", "CK_Equipments_TypeOrModel_ExactlyOne" }, names);   // a new check needs a case below

            var f = await NewFixtureAsync();
            var both = Body(f); both["equipmentModelId"] = f.ModelId;
            var neither = Body(f); neither["equipmentTypeId"] = null;
            foreach (var body in new[] { both, neither })
            {
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                AssertPersianValidation(await ReadJsonAsync(response), "equipmentTypeId");

                var id = await CreatedIdAsync(Body(f));
                var update = UpdateBody(body);
                var updated = await SendAdminAsync(HttpMethod.Put, $"/api/Equipment/{id}", update);
                Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
                AssertPersianValidation(await ReadJsonAsync(updated), "equipmentTypeId");
            }

            // CK_Equipments_Location_ExactlyOne (and its twin on the history): a branch OR a department, on create and on move
            var bothHolders = Body(f); bothHolders["currentOrganizationId"] = 1;
            var noHolder = Body(f); noHolder.Remove("currentSupportingDepartmentId");
            foreach (var body in new[] { bothHolders, noHolder })
            {
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                AssertPersianValidation(await ReadJsonAsync(response), "currentOrganizationId");
            }
            var movable = await CreatedIdAsync(Body(f));
            foreach (var move in new object[] { new { currentOrganizationId = 1, currentSupportingDepartmentId = f.DepartmentId }, new { description = "nowhere" } })
            {
                var response = await SendAdminAsync(HttpMethod.Post, $"/api/Equipment/{movable}/move", move);
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                AssertPersianValidation(await ReadJsonAsync(response), "currentOrganizationId");
            }
            Assert.Equal(new[] { "CK_EquipmentLocationHistories_Location_ExactlyOne" },
                Model().FindEntityType(typeof(EquipmentLocationHistory))!.GetCheckConstraints().Select(c => c.Name).ToArray());

            var bad = Body(f); bad["owner"] = 7;
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAdminAsync(HttpMethod.Post, "/api/Equipment", bad)).StatusCode);   // CK_Equipments_Owner (see EquipmentOwnerTests)
        }

        // ------------------------------------------------------------------------------------ foreign keys

        [Fact]
        public async Task Every_foreign_key_is_checked_and_an_unknown_id_is_a_persian_404()
        {
            var foreignKeys = Equipment().GetForeignKeys().SelectMany(k => k.Properties).Select(p => p.Name).Distinct().OrderBy(n => n).ToArray();
            Assert.Equal(new[] { "CurrentOrganizationId", "CurrentSupportingDepartmentId", "EquipmentModelId", "EquipmentStatusId", "EquipmentTypeId", "TechnicianUserId" }, foreignKeys);

            var f = await NewFixtureAsync();
            foreach (var field in new[] { "equipmentTypeId", "equipmentStatusId", "currentSupportingDepartmentId", "currentOrganizationId", "technicianUserId", "equipmentModelId" })
            {
                var body = Body(f);
                if (field == "equipmentModelId") body["equipmentTypeId"] = null;
                if (field == "currentOrganizationId") body.Remove("currentSupportingDepartmentId");      // a branch OR a department
                body[field] = 99999999;
                var response = await SendAdminAsync(HttpMethod.Post, "/api/Equipment", body);
                Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"{field} -> {(int)response.StatusCode}");
                var json = await ReadJsonAsync(response);
                Assert.Equal("parent_not_found", ErrorCodeOf(json));
                Assert.Matches("[؀-ۿ]", json.GetProperty("message").GetString()!);
            }
        }

        // ------------------------------------------------------------------------------------ unique indexes

        [Fact]
        public void Every_unique_index_has_a_message_for_the_moment_the_database_is_the_one_that_catches_it()
        {
            var unique = Equipment().GetIndexes().Where(i => i.IsUnique).Select(i => i.GetDatabaseName()!).OrderBy(n => n).ToArray();
            Assert.Equal(new[] { "UX_Equipments_AssetNumber", "UX_Equipments_SerialNumber" }, unique);   // the Yaas code is deliberately not unique

            foreach (var index in unique)
            {
                Assert.True(UniqueConstraintMessages.ByIndex.ContainsKey(index), $"{index} has no entry in UniqueConstraintMessages");
                var (message, code) = UniqueConstraintMessages.ByIndex[index];
                Assert.Matches("[؀-ۿ]", message);
                Assert.StartsWith("duplicate_", code);
            }

            // the text SQL Server gives for a duplicate key names the index: that is what picks the message
            var sqlText = "Cannot insert duplicate key row in object 'dbo.Equipments' with unique index 'UX_Equipments_SerialNumber'. The duplicate key value is (A1).";
            Assert.Equal("duplicate_serial_number", UniqueConstraintMessages.Find(sqlText)!.Value.Code);
            Assert.Equal("duplicate_asset_number", UniqueConstraintMessages.Find(sqlText.Replace("SerialNumber", "AssetNumber"))!.Value.Code);
            Assert.Null(UniqueConstraintMessages.Find("Cannot insert duplicate key row in object 'dbo.Brands' with unique index 'UX_Brands_Title'."));
        }
    }
}
