using ClosedXML.Excel;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>The users Excel file: the same rows, filters and order as the users list, in the standard layout.</summary>
    public class UsersExportTests : ApiTestBase
    {
        public UsersExportTests(EmsApiFactory factory) : base(factory) { }

        private string? _admin;
        private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
        private static string Uniq(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..28];
        private async Task<HttpResponseMessage> AsAdminAsync(HttpMethod method, string url, object? body = null) => await SendAsync(method, url, body, bearer: await AdminAsync());
        private static long Id(JsonElement e) => e.GetProperty("id").GetInt64();

        private async Task<long> NewUserAsync(string tag, string suffix, object? extra = null)
        {
            var body = new Dictionary<string, object?>
            {
                ["userName"] = $"ux_{tag}_{suffix}", ["password"] = "Passw0rd!x", ["firstName"] = "علی", ["lastName"] = $"رضایی{suffix}",
                ["personnelCode"] = $"00{suffix}{tag}", ["nationalCode"] = "00" + Random.Shared.NextInt64(10000000, 99999999), ["phoneNumber"] = "0912" + Random.Shared.NextInt64(1000000, 9999999), ["email"] = $"u{suffix}.{tag}@example.com",
            };
            if (extra is not null)
                foreach (var p in extra.GetType().GetProperties())
                    body[char.ToLowerInvariant(p.Name[0]) + p.Name[1..]] = p.GetValue(extra);
            var response = await AsAdminAsync(HttpMethod.Post, "/api/Users", body);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return Id(await ReadDataAsync(response));
        }

        private async Task<(List<string> Headers, XLWorkbook Book, string FileName)> ExportAsync(string query)
        {
            var response = await AsAdminAsync(HttpMethod.Get, $"/api/Users/export?{query}");
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"export -> {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
            Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType!.MediaType);
            var fileName = response.Content.Headers.ContentDisposition!.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName!;
            var book = new XLWorkbook(new MemoryStream(await response.Content.ReadAsByteArrayAsync()));
            var sheet = book.Worksheet(1);
            var headers = Enumerable.Range(1, sheet.LastColumnUsed()!.ColumnNumber()).Select(i => sheet.Cell(1, i).GetString()).ToList();
            return (headers, book, fileName);
        }

        [Fact]
        public async Task The_department_city_and_province_cells_name_every_department_of_a_user()
        {
            var tag = Guid.NewGuid().ToString("N")[..6];
            var city = await CatalogFixtures.EnsureCityAsync(this);
            var titleA = "ستاد آ " + tag; var titleB = "ستاد ب " + tag;
            var a = await CatalogFixtures.PostAsync(this, "/api/SupportingDepartment", new { cityId = city, title = titleA }, "id");
            var b = await CatalogFixtures.PostAsync(this, "/api/SupportingDepartment", new { cityId = city, title = titleB }, "id");
            await NewUserAsync(tag, "1", new { SupportingDepartmentIds = new[] { b, a } });
            await NewUserAsync(tag, "2");

            var (headers, book, _) = await ExportAsync($"search=ux_{tag}");
            using var _ = book;
            var sheet = book.Worksheet(1);
            string Cell(int row, string header) => sheet.Cell(row, headers.IndexOf(header) + 1).GetString();

            var rows = Enumerable.Range(2, 2).ToDictionary(r => Cell(r, "نام کاربری"), r => r);
            Assert.Equal($"{titleA}، {titleB}", Cell(rows[$"ux_{tag}_1"], "دپارتمان پشتیبانی"));      // every department, ordered by title, in one cell
            Assert.Equal(string.Empty, Cell(rows[$"ux_{tag}_2"], "دپارتمان پشتیبانی"));
            Assert.Equal(1, Cell(rows[$"ux_{tag}_1"], "شهر").Split('،').Length);                       // the city is named once, not once per department
        }

        [Fact]
        public async Task Export_is_a_standard_xlsx_that_shows_what_the_list_shows_with_roles_skills_and_text_codes()
        {
            var tag = Guid.NewGuid().ToString("N")[..6];
            var admin = await AdminAsync();
            var post = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Post, "/api/Post", new { title = Uniq("post"), code = "P" + tag[..4] }))).GetProperty("id").GetInt64();
            var role = await CreateRoleAsync(admin, "ux_role_" + tag, "Skills.View");
            var skillA = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Post, "/api/Skills", new { title = "جوشکاری " + tag }))).GetProperty("id").GetInt64();
            var skillB = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Post, "/api/Skills", new { title = "برق " + tag }))).GetProperty("id").GetInt64();

            var first = await NewUserAsync(tag, "1", new { PostId = post, RoleIds = new[] { role }, SkillIds = new[] { skillA, skillB } });
            await NewUserAsync(tag, "2");
            var other = await NewUserAsync(Guid.NewGuid().ToString("N")[..6], "3");

            var (headers, book, fileName) = await ExportAsync($"search=ux_{tag}");
            using var _ = book;
            Assert.Matches(@"^کاربران-\d{4}-\d{2}-\d{2}\.xlsx$", fileName);
            var sheet = book.Worksheet(1);
            Assert.Equal("کاربران", sheet.Name);
            Assert.True(sheet.RightToLeft);
            Assert.Equal(new[] { "شناسه", "نام کاربری", "نام", "نام خانوادگی", "کد پرسنلی", "کد ملی", "تلفن", "ایمیل", "سمت", "دپارتمان پشتیبانی", "شهر", "استان", "نقش‌ها", "مهارت‌ها", "فعال" }, headers);

            string Cell(int row, string header) => sheet.Cell(row, headers.IndexOf(header) + 1).GetString();

            Assert.Equal(3, sheet.LastRowUsed()!.RowNumber());                       // header + the two users of this tag; the third is not matched
            Assert.DoesNotContain(Enumerable.Range(2, 2), r => Cell(r, "شناسه") == other.ToString());

            // every cell is the value the list returns for the same user
            var listed = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/Users?search=ux_{tag}"))).EnumerateArray().ToList();
            Assert.Equal(2, listed.Count);
            for (var i = 0; i < 2; i++)
            {
                var item = listed[i];
                var row = i + 2;
                Assert.Equal(Id(item).ToString(), Cell(row, "شناسه"));
                Assert.Equal(item.GetProperty("userName").GetString(), Cell(row, "نام کاربری"));
                Assert.Equal(item.GetProperty("firstName").GetString(), Cell(row, "نام"));
                Assert.Equal(item.GetProperty("lastName").GetString(), Cell(row, "نام خانوادگی"));
                Assert.Equal(item.GetProperty("personnelCode").GetString(), Cell(row, "کد پرسنلی"));
                Assert.Equal(item.GetProperty("nationalCode").GetString(), Cell(row, "کد ملی"));          // text: the leading zeros survive
                Assert.Equal(item.GetProperty("phoneNumber").GetString(), Cell(row, "تلفن"));
                Assert.Equal(item.GetProperty("email").GetString(), Cell(row, "ایمیل"));
                Assert.Equal(item.GetProperty("postTitle").ValueKind == JsonValueKind.Null ? "" : item.GetProperty("postTitle").GetString(), Cell(row, "سمت"));
                Assert.Equal("بله", Cell(row, "فعال"));
                Assert.Equal(string.Join("، ", item.GetProperty("roles").EnumerateArray().Select(r => r.GetProperty("name").GetString())), Cell(row, "نقش‌ها"));
                Assert.Equal(string.Join("، ", item.GetProperty("skills").EnumerateArray().Select(s => s.GetProperty("title").GetString())), Cell(row, "مهارت‌ها"));
            }

            var firstRow = Enumerable.Range(2, 2).First(r => Cell(r, "شناسه") == first.ToString());
            Assert.StartsWith("00", Cell(firstRow, "کد ملی"));
            Assert.Equal("ux_role_" + tag, Cell(firstRow, "نقش‌ها"));
            Assert.Equal($"برق {tag}، جوشکاری {tag}", Cell(firstRow, "مهارت‌ها"));          // alphabetical, both skills in one cell
            Assert.False(string.IsNullOrEmpty(Cell(firstRow, "سمت")));
            Assert.Equal(sheet.Cell(firstRow, headers.IndexOf("کد ملی") + 1).DataType, XLDataType.Text);
        }

        [Fact]
        public async Task Export_and_list_agree_on_which_users_for_every_kind_of_filter_and_the_file_has_no_secrets()
        {
            var tag = Guid.NewGuid().ToString("N")[..6];
            var admin = await AdminAsync();
            var role = await CreateRoleAsync(admin, "ux_r_" + tag, "Skills.View");
            var a = await NewUserAsync(tag, "1", new { RoleIds = new[] { role } });
            var b = await NewUserAsync(tag, "2");
            var c = await NewUserAsync(tag, "3");
            Assert.Equal(HttpStatusCode.OK, (await AsAdminAsync(HttpMethod.Delete, $"/api/Users/{c}")).StatusCode);   // deactivated

            foreach (var query in new[]
            {
                $"search=ux_{tag}",
                $"search=ux_{tag}&isActive=false",
                $"search=ux_{tag}&isActive=true",
                $"search=ux_{tag}&roleId={role}",
                $"search=ux_{tag}&hasRole=false",
                $"search=ux_{tag}&hasRole=true",
                $"search=ux_{tag}&hasSkill=true",
                $"userName=ux_{tag}_2",
                $"search=no-such-{tag}",
            })
            {
                var listed = (await ReadDataAsync(await AsAdminAsync(HttpMethod.Get, $"/api/Users?pageSize=100&{query}"))).EnumerateArray().Select(Id).ToList();
                var (headers, book, _) = await ExportAsync(query);
                using var _b = book;
                var sheet = book.Worksheet(1);
                var exported = Enumerable.Range(2, Math.Max(0, (sheet.LastRowUsed()?.RowNumber() ?? 1) - 1)).Select(r => long.Parse(sheet.Cell(r, headers.IndexOf("شناسه") + 1).GetString())).ToList();
                Assert.True(listed.SequenceEqual(exported), $"'{query}': list [{string.Join(",", listed)}] vs file [{string.Join(",", exported)}]");
            }

            var (allHeaders, all, _) = await ExportAsync($"search=ux_{tag}");
            using (all)
            {
                var text = string.Join(" ", Enumerable.Range(1, all.Worksheet(1).LastColumnUsed()!.ColumnNumber()).Select(i => all.Worksheet(1).Cell(1, i).GetString())).ToLowerInvariant();
                foreach (var secret in new[] { "password", "hash", "security", "stamp", "رمز", "token" })
                    Assert.DoesNotContain(secret, text);
                Assert.DoesNotContain(allHeaders, h => h.Contains("رمز"));
            }
            _ = (a, b);
        }

        [Fact]
        public async Task Export_of_a_filter_that_matches_nothing_is_a_valid_file_and_a_bad_filter_is_a_json_error()
        {
            var (_, book, _) = await ExportAsync($"search=nothing-{Guid.NewGuid():N}");
            using (book)
                Assert.Equal(1, book.Worksheet(1).LastRowUsed()!.RowNumber());

            var bad = await AsAdminAsync(HttpMethod.Get, "/api/Users/export?roleId=0");
            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
            Assert.Equal("bad_request", ErrorCodeOf(await ReadJsonAsync(bad)));
        }

        [Fact]
        public async Task Export_needs_the_same_permission_as_the_list()
        {
            var admin = await AdminAsync();
            async Task<string> TokenWithAsync(params string[] permissions)
            {
                var name = "uxp_" + Guid.NewGuid().ToString("N")[..8];
                var userId = await CreateUserAsync(admin, name, "Passw0rd!x");
                await AssignRoleAsync(admin, userId, await CreateRoleAsync(admin, "role_" + name, permissions));
                return (await LoginAsync(name, "Passw0rd!x")).AccessToken;
            }

            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Users/export", await TokenWithAsync("Skills.View")));
            Assert.Equal(HttpStatusCode.Forbidden, await StatusOfAsync(HttpMethod.Get, "/api/Users/export", await TokenWithAsync("Users.Create")));
            Assert.Equal(HttpStatusCode.OK, await StatusOfAsync(HttpMethod.Get, "/api/Users/export", await TokenWithAsync("Users.View")));
            Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, "/api/Users/export")).StatusCode);
        }
    }
}
