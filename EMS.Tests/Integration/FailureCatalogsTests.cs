using EMS.Domain;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>What is specific to the failure catalogs: the type's category, the action's description and the system-rows guarantee.</summary>
    public class FailureCatalogsTests : ApiTestBase
    {
        public FailureCatalogsTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private async Task<HttpResponseMessage> SendAdminAsync(HttpMethod method, string url, object? body = null) =>
            await SendAsync(method, url, body, bearer: await AdminAsync());
        private async Task<long> NewCategoryAsync() =>
            await CatalogFixtures.PostAsync(this, "/api/FailureCategory", new { title = Uniq("fc") }, "id");

        // ------------------------------------------------------------------------------------ failure type

        [Fact]
        public async Task A_failure_type_needs_an_existing_active_category()
        {
            var unknown = await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = 99999999, title = Uniq("t") });
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal("parent_not_found", ErrorCodeOf(await ReadJsonAsync(unknown)));

            var zero = await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = 0, title = Uniq("t") });
            Assert.Equal(HttpStatusCode.BadRequest, zero.StatusCode);
            Assert.Contains("failureCategoryId", ErrorFieldsOf(await ReadJsonAsync(zero)));

            var category = await NewCategoryAsync();
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureCategory/{category}")).StatusCode);
            var inactive = await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = category, title = Uniq("t") });
            Assert.Equal(HttpStatusCode.BadRequest, inactive.StatusCode);
            Assert.Equal("parent_inactive", ErrorCodeOf(await ReadJsonAsync(inactive)));
        }

        [Fact]
        public async Task The_title_is_unique_per_category_and_the_list_filters_by_category()
        {
            var first = await NewCategoryAsync();
            var second = await NewCategoryAsync();
            var title = Uniq("same");

            var a = await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = first, title });
            Assert.Equal(HttpStatusCode.Created, a.StatusCode);
            var duplicate = await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = first, title });
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Equal("duplicate_title", ErrorCodeOf(await ReadJsonAsync(duplicate)));
            Assert.Equal(HttpStatusCode.Created, (await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = second, title })).StatusCode);

            var list = DataOf(await ReadJsonAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureType?failureCategoryId={first}")));
            Assert.Equal(1, list.GetArrayLength());
            Assert.Equal(first, list[0].GetProperty("failureCategoryId").GetInt64());
        }

        [Fact]
        public async Task A_category_with_active_types_cannot_be_deactivated()
        {
            var category = await NewCategoryAsync();
            var type = (await ReadDataAsync(await SendAdminAsync(HttpMethod.Post, "/api/FailureType", new { failureCategoryId = category, title = Uniq("t") }))).GetProperty("id").GetInt64();

            var blocked = await SendAdminAsync(HttpMethod.Delete, $"/api/FailureCategory/{category}");
            Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
            Assert.Equal("has_active_dependents", ErrorCodeOf(await ReadJsonAsync(blocked)));

            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureType/{type}")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAdminAsync(HttpMethod.Delete, $"/api/FailureCategory/{category}")).StatusCode);
        }

        // ------------------------------------------------------------------------------------ action

        [Fact]
        public async Task An_action_keeps_its_description_and_clears_nothing_when_it_is_not_sent()
        {
            var created = await SendAdminAsync(HttpMethod.Post, "/api/Action", new { title = Uniq("act"), description = "تعویض قطعه" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var json = await ReadDataAsync(created);
            Assert.Equal("تعویض قطعه", json.GetProperty("description").GetString());

            var id = json.GetProperty("id").GetInt64();
            var renamed = await SendAdminAsync(HttpMethod.Put, $"/api/Action/{id}", new { title = Uniq("act2"), isActive = true });
            Assert.Equal("تعویض قطعه", (await ReadDataAsync(renamed)).GetProperty("description").GetString());

            var tooLong = await SendAdminAsync(HttpMethod.Post, "/api/Action", new { title = Uniq("act3"), description = new string('x', 501) });
            Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
            Assert.Contains("description", ErrorFieldsOf(await ReadJsonAsync(tooLong)));
        }

        // ------------------------------------------------------------------------------------ seeded rows

        [Fact]
        public async Task The_seeded_rows_exist_with_their_persian_titles()
        {
            foreach (var (code, title) in ReportTypeCatalog.Titles)
            {
                var json = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/ReportType/{ReportTypeCatalog.IdOf(code)}"));
                Assert.Equal(code.ToString(), json.GetProperty("code").GetString());
                Assert.Equal(title, json.GetProperty("title").GetString());
            }
            foreach (var (code, title) in FailureReportStatusCatalog.Titles)
            {
                var json = await ReadDataAsync(await SendAdminAsync(HttpMethod.Get, $"/api/FailureReportStatus/{FailureReportStatusCatalog.IdOf(code)}"));
                Assert.Equal(code.ToString(), json.GetProperty("code").GetString());
                Assert.Equal(title, json.GetProperty("title").GetString());
            }
        }

        [Fact]
        public async Task Reading_is_open_to_any_signed_in_user_but_writing_needs_the_permission()
        {
            var admin = await AdminAsync();
            var userName = "fcplain" + Guid.NewGuid().ToString("N")[..6];
            await CreateUserAsync(admin, userName, "Passw0rd!x");
            var plain = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            foreach (var route in new[] { "FailureType", "ReportType", "Action", "FailureReportStatus" })
            {
                Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/{route}", bearer: plain)).StatusCode);
                Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Post, $"/api/{route}", new { title = Uniq("x") }, bearer: plain)).StatusCode);
                Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, $"/api/{route}")).StatusCode);
            }
        }

        // ------------------------------------------------------------------------------------ the guard

        [Fact]
        public async Task The_guard_restores_system_rows_that_were_removed_deactivated_or_recoded_and_keeps_edited_titles()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            var guard = scope.ServiceProvider.GetRequiredService<ISystemCatalogsGuard>();
            Assert.Equal((0, 0), await guard.EnsureAsync());   // intact: nothing to do

            var failure = await db.ReportTypes.SingleAsync(r => r.ReportTypeId == (long)ReportTypeCode.Failure);
            var incident = await db.ReportTypes.SingleAsync(r => r.ReportTypeId == (long)ReportTypeCode.Incident);
            var closed = await db.FailureReportStatuses.SingleAsync(s => s.FailureReportStatusId == (long)FailureReportStatusCode.Closed);
            var edited = Uniq("edited");
            failure.IsActive = false; failure.Title = edited;
            incident.Code = "Hacked";
            db.FailureReportStatuses.Remove(closed);
            await db.SaveChangesAsync();

            Assert.Equal((1, 2), await guard.EnsureAsync());
            db.ChangeTracker.Clear();

            var types = await db.ReportTypes.IncludingInactive().Where(r => r.ReportTypeId <= 2).OrderBy(r => r.ReportTypeId).ToListAsync();
            Assert.Equal(new[] { "Failure", "Incident" }, types.Select(t => t.Code).ToArray());
            Assert.All(types, t => Assert.True(t.IsActive));
            Assert.Equal(edited, types[0].Title);                                 // an edited title is never overwritten
            var status = await db.FailureReportStatuses.IncludingInactive().SingleAsync(s => s.FailureReportStatusId == 3);
            Assert.Equal("Closed", status.Code);
            Assert.Equal(FailureReportStatusCatalog.Titles[FailureReportStatusCode.Closed], status.Title);   // re-created with the initial title
            Assert.Equal((0, 0), await guard.EnsureAsync());                      // idempotent

            var restored = await db.ReportTypes.SingleAsync(r => r.ReportTypeId == (long)ReportTypeCode.Failure);
            restored.Title = ReportTypeCatalog.Titles[ReportTypeCode.Failure]; await db.SaveChangesAsync();   // leave the shared database as found
        }
    }
}
