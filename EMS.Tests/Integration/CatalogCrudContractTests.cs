using EMS.Application.Common.Models;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>
    /// The observable behaviour of every catalog endpoint, run identically against all catalogs.
    /// This is the safety net for refactoring the catalog implementation: behaviour and JSON shapes must not change.
    /// </summary>
    public class CatalogCrudContractTests : ApiTestBase
    {
        public CatalogCrudContractTests(EmsApiFactory factory) : base(factory) { }

        public static IEnumerable<object[]> Cases => CatalogCase.Names;

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;

        private static string Title(string label = "t") => $"{label}-{Guid.NewGuid():N}"[..28];

        private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name[1..];

        private static HashSet<string> JsonNamesOf(Type dto) =>
            dto.GetProperties().Select(p => Camel(p.Name)).ToHashSet();

        private static HashSet<string> NamesIn(JsonElement element) =>
            element.EnumerateObject().Select(p => p.Name).ToHashSet();

        private async Task<(long Id, JsonElement Json)> CreateAsync(CatalogCase c, string title)
        {
            var body = await c.CreateBody(this, title);
            var response = await SendAsync(HttpMethod.Post, c.Route, body, bearer: await AdminAsync());
            Assert.True(response.StatusCode == HttpStatusCode.Created, $"{c.Name} create -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            var json = await ReadDataAsync(response);
            return (json.GetProperty(c.IdProperty).GetInt64(), json);
        }

        private async Task<HttpResponseMessage> UpdateAsync(CatalogCase c, long id, string? title, bool isActive = true, string? rowVersion = null, string? code = null)
        {
            var body = new Dictionary<string, object?> { ["isActive"] = isActive };
            if (title is not null) body["title"] = title;
            if (code is not null) body["code"] = code;
            if (rowVersion is not null) body["rowVersion"] = rowVersion;
            return await SendAsync(HttpMethod.Put, $"{c.Route}/{id}", body, bearer: await AdminAsync());
        }

        private async Task<JsonElement> ListAsync(CatalogCase c, string query = "")
        {
            var response = await SendAsync(HttpMethod.Get, $"{c.Route}?{query}", bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return await ReadJsonAsync(response);
        }

        private static List<string> TitlesOf(JsonElement page) =>
            DataOf(page).EnumerateArray().Select(i => i.GetProperty("title").GetString()!).ToList();

        // ------------------------------------------------------------------------------- create / read

        [Theory, MemberData(nameof(Cases))]
        public async Task Create_returns_201_with_a_location_and_the_exact_detail_shape(string name)
        {
            var c = CatalogCase.Get(name);
            var title = Title(name);
            var body = await c.CreateBody(this, title);
            var response = await SendAsync(HttpMethod.Post, c.Route, body, bearer: await AdminAsync());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var envelope = await ReadJsonAsync(response);
            Assert.True(envelope.GetProperty("success").GetBoolean());
            Assert.False(string.IsNullOrWhiteSpace(envelope.GetProperty("message").GetString()));
            var json = DataOf(envelope);
            var id = json.GetProperty(c.IdProperty).GetInt64();
            Assert.EndsWith($"{c.Route}/{id}", response.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(JsonNamesOf(c.DetailDto), NamesIn(json));
            Assert.Equal(title, json.GetProperty("title").GetString());
            Assert.True(json.GetProperty("isActive").GetBoolean());

            var get = await SendAsync(HttpMethod.Get, $"{c.Route}/{id}", bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(title, (await ReadDataAsync(get)).GetProperty("title").GetString());
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task An_unknown_id_is_404_with_a_message(string name)
        {
            var c = CatalogCase.Get(name);
            var response = await SendAsync(HttpMethod.Get, $"{c.Route}/987654321", bearer: await AdminAsync());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var envelope = await ReadJsonAsync(response);
            Assert.False(envelope.GetProperty("success").GetBoolean());
            Assert.Equal("not_found", ErrorCodeOf(envelope));
            Assert.False(string.IsNullOrWhiteSpace(envelope.GetProperty("message").GetString()));
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task A_missing_title_is_a_400_validation_error_listing_the_problem(string name)
        {
            var c = CatalogCase.Get(name);
            var body = await c.CreateBody(this, "x");
            body["title"] = "";
            var response = await SendAsync(HttpMethod.Post, c.Route, body, bearer: await AdminAsync());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var envelope = await ReadJsonAsync(response);
            Assert.Equal("validation_failed", ErrorCodeOf(envelope));
            Assert.Contains("title", ErrorFieldsOf(envelope));
        }

        // ------------------------------------------------------------------------------- uniqueness

        [Theory, MemberData(nameof(Cases))]
        public async Task A_duplicate_title_is_409_ignoring_case_and_is_free_again_after_the_record_is_deleted(string name)
        {
            var c = CatalogCase.Get(name);
            var title = Title("Dup");
            var (id, _) = await CreateAsync(c, title);

            var same = await SendAsync(HttpMethod.Post, c.Route, await c.CreateBody(this, title), bearer: await AdminAsync());
            var upper = await SendAsync(HttpMethod.Post, c.Route, await c.CreateBody(this, title.ToUpperInvariant()), bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.Conflict, same.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, upper.StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"{c.Route}/{id}", bearer: await AdminAsync())).StatusCode);
            var again = await SendAsync(HttpMethod.Post, c.Route, await c.CreateBody(this, title), bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task Renaming_onto_another_active_records_title_is_409(string name)
        {
            var c = CatalogCase.Get(name);
            var (_, _) = await CreateAsync(c, Title("RenA"));
            var taken = Title("RenB");
            await CreateAsync(c, taken);
            var (otherId, _) = await CreateAsync(c, Title("RenC"));

            Assert.Equal(HttpStatusCode.Conflict, (await UpdateAsync(c, otherId, taken)).StatusCode);
        }

        // ------------------------------------------------------------------------------- list

        [Theory, MemberData(nameof(Cases))]
        public async Task The_list_is_paged_filtered_sorted_and_has_the_exact_item_shape(string name)
        {
            var c = CatalogCase.Get(name);
            var prefix = Title("Lst");
            var ids = new List<long>();
            foreach (var suffix in new[] { "B", "A", "C" })
                ids.Add((await CreateAsync(c, prefix + suffix)).Id);

            // filter by title + sort descending
            var page = await ListAsync(c, $"title={prefix}&sortBy=title&sortDirection=desc&pageSize=10");
            Assert.Equal(new[] { prefix + "C", prefix + "B", prefix + "A" }, TitlesOf(page));
            Assert.Equal(3, PaginationOf(page).GetProperty("totalCount").GetInt32());
            Assert.Equal(JsonNamesOf(c.ListItemDto), NamesIn(DataOf(page)[0]));

            // ascending
            Assert.Equal(new[] { prefix + "A", prefix + "B", prefix + "C" }, TitlesOf(await ListAsync(c, $"title={prefix}&sortBy=title&pageSize=10")));

            // default order is by id (creation order)
            var byId = DataOf(page).EnumerateArray().Select(i => i.GetProperty(c.IdProperty).GetInt64()).OrderBy(x => x).ToList();
            var defaultOrder = DataOf(await ListAsync(c, $"title={prefix}&pageSize=10")).EnumerateArray().Select(i => i.GetProperty(c.IdProperty).GetInt64()).ToList();
            Assert.Equal(byId, defaultOrder);
            Assert.Equal(ids.OrderBy(x => x), defaultOrder);

            // paging
            var second = await ListAsync(c, $"title={prefix}&sortBy=title&pageSize=2&pageNumber=2");
            Assert.Equal(new[] { prefix + "C" }, TitlesOf(second));
            Assert.Equal(3, PaginationOf(second).GetProperty("totalCount").GetInt32());
            Assert.Equal(2, PaginationOf(second).GetProperty("pageNumber").GetInt32());
            Assert.Equal(2, PaginationOf(second).GetProperty("pageSize").GetInt32());

            // the envelope and its pagination have exactly the documented members
            Assert.Equal(typeof(ApiResponse<>).GetProperties().Select(p => Camel(p.Name)).ToHashSet(), NamesIn(page));
            Assert.Equal(typeof(ApiPagination).GetProperties().Select(p => Camel(p.Name)).ToHashSet(), NamesIn(PaginationOf(page)));
            Assert.Equal(JsonValueKind.Null, page.GetProperty("error").ValueKind);
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task The_list_rejects_an_invalid_page_size_or_number(string name)
        {
            var c = CatalogCase.Get(name);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Get, $"{c.Route}?pageSize=101", bearer: await AdminAsync())).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await SendAsync(HttpMethod.Get, $"{c.Route}?pageNumber=0", bearer: await AdminAsync())).StatusCode);
        }

        // ------------------------------------------------------------------------------- update

        [Theory, MemberData(nameof(Cases))]
        public async Task Update_changes_the_title_keeps_omitted_fields_and_returns_the_detail(string name)
        {
            var c = CatalogCase.Get(name);
            var original = Title("UpdA");
            var (id, created) = await CreateAsync(c, original);

            var renamed = Title("UpdB");
            var update = await UpdateAsync(c, id, renamed);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            var envelope = await ReadJsonAsync(update);
            Assert.False(string.IsNullOrWhiteSpace(envelope.GetProperty("message").GetString()));
            var json = DataOf(envelope);
            Assert.Equal(renamed, json.GetProperty("title").GetString());
            Assert.Equal(JsonNamesOf(c.DetailDto), NamesIn(json));
            Assert.NotEqual(created.GetProperty("rowVersion").GetString(), json.GetProperty("rowVersion").GetString());

            // title omitted => unchanged
            var keep = await ReadDataAsync(await UpdateAsync(c, id, title: null));
            Assert.Equal(renamed, keep.GetProperty("title").GetString());
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task Update_of_an_unknown_id_is_404(string name)
        {
            var c = CatalogCase.Get(name);
            Assert.Equal(HttpStatusCode.NotFound, (await UpdateAsync(c, 987654321, Title())).StatusCode);
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task A_stale_row_version_is_409_a_current_one_works_and_no_version_still_works(string name)
        {
            var c = CatalogCase.Get(name);
            var (id, created) = await CreateAsync(c, Title("Ver"));
            var v0 = created.GetProperty("rowVersion").GetString()!;

            var first = await UpdateAsync(c, id, Title("VerA"), rowVersion: v0);
            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            var v1 = (await ReadDataAsync(first)).GetProperty("rowVersion").GetString()!;

            Assert.Equal(HttpStatusCode.Conflict, (await UpdateAsync(c, id, Title("VerB"), rowVersion: v0)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await UpdateAsync(c, id, Title("VerC"), rowVersion: v1)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await UpdateAsync(c, id, Title("VerD"))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await UpdateAsync(c, id, Title("VerE"), rowVersion: "garbage")).StatusCode);
        }

        // ------------------------------------------------------------------------------- validation rules

        private static HashSet<string> ErrorFields(JsonElement envelope) => ErrorFieldsOf(envelope).ToHashSet();

        [Theory, MemberData(nameof(Cases))]
        public async Task An_oversized_title_is_rejected_on_create_and_update_but_the_maximum_length_is_accepted(string name)
        {
            var c = CatalogCase.Get(name);

            var tooLong = await c.CreateBody(this, new string('a', 101));
            var create = await SendAsync(HttpMethod.Post, c.Route, tooLong, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
            Assert.Contains("title", ErrorFields(await ReadJsonAsync(create)));

            var (id, _) = await CreateAsync(c, new string('b', 100) + "");   // exactly the maximum is fine
            var update = await UpdateAsync(c, id, new string('c', 101));
            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
            Assert.Contains("title", ErrorFields(await ReadJsonAsync(update)));
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task An_update_with_a_non_positive_id_is_a_validation_error_on_the_id_field(string name)
        {
            var c = CatalogCase.Get(name);
            var response = await UpdateAsync(c, 0, Title("Id"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("id", ErrorFields(await ReadJsonAsync(response)));
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task An_invalid_sort_direction_is_a_validation_error(string name)
        {
            var c = CatalogCase.Get(name);
            var response = await SendAsync(HttpMethod.Get, $"{c.Route}?sortDirection=sideways", bearer: await AdminAsync());

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("sortDirection", ErrorFields(await ReadJsonAsync(response)));
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task A_code_longer_than_its_column_is_a_validation_error_for_every_coded_catalog_it_used_to_be_unchecked_for_brands_and_models(string name)
        {
            var c = CatalogCase.Get(name);
            if (!c.HasCode)
                return;

            var body = await c.CreateBody(this, Title("Code"));
            body["code"] = new string('x', 21);
            var create = await SendAsync(HttpMethod.Post, c.Route, body, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
            Assert.Contains("code", ErrorFields(await ReadJsonAsync(create)));

            var (id, _) = await CreateAsync(c, Title("Code2"));
            var update = await UpdateAsync(c, id, title: null, code: new string('y', 21));
            Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
            Assert.Contains("code", ErrorFields(await ReadJsonAsync(update)));

            // the maximum itself is accepted
            Assert.Equal(HttpStatusCode.OK, (await UpdateAsync(c, id, title: null, code: new string('z', 20))).StatusCode);
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task Validation_messages_name_the_field_and_the_catalog(string name)
        {
            var c = CatalogCase.Get(name);
            var page = await SendAsync(HttpMethod.Get, $"{c.Route}?pageSize=101", bearer: await AdminAsync());
            var pageMessage = (await ReadJsonAsync(page)).GetProperty("error").GetProperty("details").EnumerateArray()
                .First(d => d.GetProperty("field").GetString() == "pageSize").GetProperty("message").GetString();
            Assert.Contains("اندازه صفحه", pageMessage);        // the message used to be a fragment without a subject

            var update = await UpdateAsync(c, 0, Title("Msg"));
            var idMessage = (await ReadJsonAsync(update)).GetProperty("error").GetProperty("details")[0].GetProperty("message").GetString();
            Assert.Contains("شناسه", idMessage);
        }

        [Theory]
        [InlineData("EquipmentType")]
        [InlineData("EquipmentModel")]
        [InlineData("SupportingDepartment")]
        public async Task A_non_positive_parent_id_is_a_validation_error_on_create(string name)
        {
            var c = CatalogCase.Get(name);
            var body = await c.CreateBody(this, Title("Par"));
            var parentKey = body.Keys.First(k => k.EndsWith("Id", StringComparison.Ordinal));
            body[parentKey] = 0;

            var response = await SendAsync(HttpMethod.Post, c.Route, body, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains(parentKey, ErrorFields(await ReadJsonAsync(response)));
        }

        // ------------------------------------------------------------------------------- behaviour fixed by the refactoring

        [Theory, MemberData(nameof(Cases))]
        public async Task A_one_character_title_is_applied_it_used_to_be_silently_ignored(string name)
        {
            var c = CatalogCase.Get(name);
            var (id, _) = await CreateAsync(c, Title("One"));

            // Titles are unique among active records, so pick a random rare single character (retry on the unlikely clash).
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var single = ((char)(0xFB50 + Random.Shared.Next(0, 0x2AF))).ToString();
                var update = await UpdateAsync(c, id, single);
                if (update.StatusCode == HttpStatusCode.Conflict)
                    continue;

                Assert.Equal(HttpStatusCode.OK, update.StatusCode);
                Assert.Equal(single, (await ReadDataAsync(update)).GetProperty("title").GetString());
                return;
            }

            Assert.Fail("Could not find a free single-character title in 5 attempts.");
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task A_one_character_code_is_applied_it_used_to_be_silently_ignored(string name)
        {
            var c = CatalogCase.Get(name);
            if (!c.HasCode)
                return;

            var (id, _) = await CreateAsync(c, Title("OneCode"));
            var update = await UpdateAsync(c, id, title: null, code: "Z");

            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            Assert.Equal("Z", (await ReadDataAsync(update)).GetProperty("code").GetString());
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task A_code_is_trimmed_on_create_like_on_update(string name)
        {
            var c = CatalogCase.Get(name);
            if (!c.HasCode)
                return;

            var body = await c.CreateBody(this, Title("Trim"));
            body["code"] = "  ab  ";
            var response = await SendAsync(HttpMethod.Post, c.Route, body, bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal("ab", (await ReadDataAsync(response)).GetProperty("code").GetString());
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task A_deactivated_record_does_not_reserve_its_title_not_even_for_a_rename_but_cannot_be_restored_onto_a_taken_title(string name)
        {
            var c = CatalogCase.Get(name);
            var activeTitle = Title("Act");
            await CreateAsync(c, activeTitle);
            var (id, _) = await CreateAsync(c, Title("Gone"));
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"{c.Route}/{id}", bearer: await AdminAsync())).StatusCode);

            // staying inactive, it may carry the same title as an active record
            Assert.Equal(HttpStatusCode.OK, (await UpdateAsync(c, id, activeTitle, isActive: false)).StatusCode);
            // ...but restoring it would create a duplicate among active records
            Assert.Equal(HttpStatusCode.Conflict, (await UpdateAsync(c, id, activeTitle, isActive: true)).StatusCode);
        }

        // ------------------------------------------------------------------------------- soft delete

        [Theory, MemberData(nameof(Cases))]
        public async Task Delete_is_a_soft_delete_that_hides_the_record_from_the_default_list_but_not_from_get_or_the_inactive_list(string name)
        {
            var c = CatalogCase.Get(name);
            var title = Title("Del");
            var (id, _) = await CreateAsync(c, title);

            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"{c.Route}/{id}", bearer: await AdminAsync())).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"{c.Route}/{id}", bearer: await AdminAsync())).StatusCode); // idempotent

            Assert.DoesNotContain(title, TitlesOf(await ListAsync(c, $"title={title}")));
            Assert.Contains(title, TitlesOf(await ListAsync(c, $"title={title}&isActive=false")));
            Assert.DoesNotContain(title, TitlesOf(await ListAsync(c, $"title={title}&isActive=true")));

            var get = await SendAsync(HttpMethod.Get, $"{c.Route}/{id}", bearer: await AdminAsync());
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.False((await ReadDataAsync(get)).GetProperty("isActive").GetBoolean());

            // restoring works through update
            var restore = await UpdateAsync(c, id, title: null, isActive: true);
            Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
            Assert.Contains(title, TitlesOf(await ListAsync(c, $"title={title}")));
        }

        [Theory, MemberData(nameof(Cases))]
        public async Task Deleting_an_unknown_id_is_404(string name)
        {
            var c = CatalogCase.Get(name);
            Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Delete, $"{c.Route}/987654321", bearer: await AdminAsync())).StatusCode);
        }

        // ------------------------------------------------------------------------------- authorization

        [Theory, MemberData(nameof(Cases))]
        public async Task Every_verb_needs_its_own_permission_and_anonymous_callers_get_401(string name)
        {
            var c = CatalogCase.Get(name);
            var (id, _) = await CreateAsync(c, Title("Aut"));
            var admin = await AdminAsync();
            var userName = ("ct_" + name)[..Math.Min(18, name.Length + 3)] + Guid.NewGuid().ToString("N")[..2];
            var userId = await CreateUserAsync(admin, userName, "Passw0rd!x");
            var roleId = await CreateRoleAsync(admin, "CtRole_" + userName, $"{c.PermissionGroup}.View");
            await AssignRoleAsync(admin, userId, roleId);
            var viewer = (await LoginAsync(userName, "Passw0rd!x")).AccessToken;

            // View is enough to read...
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, c.Route, viewer));
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, $"{c.Route}/{id}", viewer));
            // ...but not to change anything
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Post, c.Route, viewer, await c.CreateBody(this, Title("Aut2"))));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Put, $"{c.Route}/{id}", viewer, new Dictionary<string, object?> { ["isActive"] = true }));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Delete, $"{c.Route}/{id}", viewer));

            // anonymous
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, c.Route)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Delete, $"{c.Route}/{id}")).StatusCode);
        }
    }
}
