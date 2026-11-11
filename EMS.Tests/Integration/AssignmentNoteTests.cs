using EMS.Application.Common.Security;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration;

public class AssignmentNoteTests : ApiTestBase
{
    public AssignmentNoteTests(EmsApiFactory factory) : base(factory) { }
    private string? _admin;
    private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
    private static string Unique() => Guid.NewGuid().ToString("N")[..12];
    private static long Id(JsonElement item) => item.GetProperty("id").GetInt64();
    private static string Version(JsonElement item) => item.GetProperty("rowVersion").GetString()!;
    private static string Notes(long assignmentId) => $"/api/TechnicianAssignment/{assignmentId}/notes";
    private static string Note(long id) => $"/api/AssignmentNote/{id}";
    private async Task<HttpResponseMessage> AdminSendAsync(HttpMethod method, string url, object? body = null) =>
        await SendAsync(method, url, body, bearer: await AdminAsync());

    private async Task<(long Id, string Token)> UserAsync(bool permissions = true)
    {
        var name = "an_" + Unique();
        var id = await CreateUserAsync(await AdminAsync(), name, "Passw0rd!x");
        if (permissions)
        {
            var role = await CreateRoleAsync(await AdminAsync(), "role_" + name,
                Permissions.AssignmentNotes.View, Permissions.AssignmentNotes.Create,
                Permissions.AssignmentNotes.Update, Permissions.AssignmentNotes.Delete, Permissions.AssignmentNotes.MarkSeen);
            await AssignRoleAsync(await AdminAsync(), id, role);
        }
        return (id, (await LoginAsync(name, "Passw0rd!x")).AccessToken);
    }
    private sealed record Fixture(long AssignmentId, long CategoryId, long TechnicianId, string TechnicianToken);
    private async Task<Fixture> FixtureAsync(long statusId = 1, long sender = 1)
    {
        var technician = await UserAsync();
        if (sender == -1) sender = technician.Id;
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
        var category = new NoteCategory { Title = "موضوع " + Unique() };
        var report = new FailureReport
        {
            Equipment = new Equipment
            {
                YaasCode = Unique(), Owner = EMS.Domain.Enums.EquipmentOwner.Company, EquipmentStatusId = 1,
                EquipmentType = new EquipmentType { Title = Unique(), EquipmentCategory = new EquipmentCategory { Title = Unique() } },
                CurrentSupportingDepartment = new SupportingDepartment
                {
                    Title = Unique(), City = new City { Title = Unique(), Province = new Province { Title = Unique() } }
                }
            },
            FailureType = new FailureType { Title = Unique(), FailureCategory = new FailureCategory { Title = Unique() } },
            Organization = new Organization
            {
                Title = Unique(), OrganizationType = new OrganizationType { Title = Unique() },
                OrganizationManagement = new OrganizationManagement { Title = Unique(), Zone = new Zone { Title = Unique() } }
            },
            ReportTypeId = 1, FailureReportStatusId = 1, ReportedByUserId = sender,
            PriorityId = 1, ReportedDateTime = DateTime.UtcNow
        };
        var assignment = new TechnicianAssignment
        {
            FailureReport = report, AssignedByUserId = sender, TechnicianUserId = technician.Id,
            TechnicianAssignmentStatusId = statusId, AssignedDateTime = DateTime.UtcNow
        };
        db.AddRange(category, assignment);
        await db.SaveChangesAsync();
        return new(assignment.TechnicianAssignmentId, category.NoteCategoryId, technician.Id, technician.Token);
    }
    private async Task<JsonElement> CreateAsync(Fixture fixture, string text = "متن یادداشت")
    {
        var response = await AdminSendAsync(HttpMethod.Post, Notes(fixture.AssignmentId), new { noteCategoryId = fixture.CategoryId, text });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await ReadDataAsync(response);
        Assert.EndsWith(Note(Id(item)), response.Headers.Location!.AbsolutePath);
        return item;
    }
    private async Task<JsonElement> SeenAsync(Fixture fixture, JsonElement note)
    {
        var response = await SendAsync(HttpMethod.Post, Note(Id(note)) + "/seen", new { rowVersion = Version(note) }, bearer: fixture.TechnicianToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadDataAsync(response);
    }

    [Fact]
    public async Task Only_sender_creates_and_only_recipient_acknowledges_reading_with_the_first_server_timestamp()
    {
        var f = await FixtureAsync();
        var before = DateTime.UtcNow;
        var note = await CreateAsync(f, "  متن فارسی\nپاراگراف دوم  ");
        Assert.Equal("متن فارسی\nپاراگراف دوم", note.GetProperty("text").GetString());
        Assert.Equal(1, note.GetProperty("createdByUserId").GetInt64());
        Assert.Equal(JsonValueKind.Null, note.GetProperty("seenDateTime").ValueKind);
        var recipientCreate = await SendAsync(HttpMethod.Post, Notes(f.AssignmentId), new { noteCategoryId = f.CategoryId, text = "متن" }, bearer: f.TechnicianToken);
        Assert.Equal(HttpStatusCode.Forbidden, recipientCreate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await AdminSendAsync(HttpMethod.Post, Note(Id(note)) + "/seen", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, Notes(f.AssignmentId), bearer: f.TechnicianToken)).StatusCode);
        var read = await ReadDataAsync(await SendAsync(HttpMethod.Get, Note(Id(note)), bearer: f.TechnicianToken));
        Assert.Equal(JsonValueKind.Null, read.GetProperty("seenDateTime").ValueKind);
        var seen = await SeenAsync(f, note);
        var timestamp = seen.GetProperty("seenDateTime").GetDateTime();
        Assert.InRange(timestamp, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        var repeated = await SeenAsync(f, seen);
        Assert.Equal(timestamp, repeated.GetProperty("seenDateTime").GetDateTime());
        Assert.Equal(Version(seen), Version(repeated));
    }

    [Fact]
    public async Task Even_a_sender_who_is_also_the_technician_must_explicitly_acknowledge_the_note()
    {
        var f = await FixtureAsync(sender: -1);
        var response = await SendAsync(HttpMethod.Post, Notes(f.AssignmentId), new { noteCategoryId = f.CategoryId, text = "متن" }, bearer: f.TechnicianToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var note = await ReadDataAsync(response);
        Assert.Equal(JsonValueKind.Null, note.GetProperty("seenDateTime").ValueKind);
        Assert.Equal(f.TechnicianId, note.GetProperty("createdByUserId").GetInt64());
        await SeenAsync(f, note);
    }

    [Fact]
    public async Task Content_or_category_change_resets_seen_but_a_noop_does_not_and_stale_versions_conflict()
    {
        var f = await FixtureAsync();
        var note = await CreateAsync(f);
        var seen = await SeenAsync(f, note);
        var unchanged = await ReadDataAsync(await AdminSendAsync(HttpMethod.Put, Note(Id(note)), new { noteCategoryId = f.CategoryId, text = "  متن یادداشت  ", rowVersion = Version(seen) }));
        Assert.Equal(seen.GetProperty("seenDateTime").GetString(), unchanged.GetProperty("seenDateTime").GetString());
        Assert.Equal(Version(seen), Version(unchanged));
        var stale = await AdminSendAsync(HttpMethod.Put, Note(Id(note)), new { noteCategoryId = f.CategoryId, text = "تغییر قدیمی", rowVersion = Version(note) });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        var edited = await ReadDataAsync(await AdminSendAsync(HttpMethod.Put, Note(Id(note)), new { noteCategoryId = f.CategoryId, text = "متن تازه", rowVersion = Version(seen) }));
        Assert.Equal(JsonValueKind.Null, edited.GetProperty("seenDateTime").ValueKind);
        var staleSeen = await SendAsync(HttpMethod.Post, Note(Id(note)) + "/seen", new { rowVersion = Version(seen) }, bearer: f.TechnicianToken);
        Assert.Equal(HttpStatusCode.Conflict, staleSeen.StatusCode);
        seen = await SeenAsync(f, edited);
        var category = await CatalogFixtures.PostAsync(this, "/api/NoteCategory", new { title = "موضوع " + Unique() }, "id");
        var recategorized = await ReadDataAsync(await AdminSendAsync(HttpMethod.Put, Note(Id(note)), new { noteCategoryId = category, text = "متن تازه", rowVersion = Version(seen) }));
        Assert.Equal(category, recategorized.GetProperty("noteCategoryId").GetInt64());
        Assert.Equal(JsonValueKind.Null, recategorized.GetProperty("seenDateTime").ValueKind);
    }

    [Fact]
    public async Task Participant_policy_applies_to_all_routes_and_authentication_and_permissions_are_still_required()
    {
        var f = await FixtureAsync();
        var note = await CreateAsync(f);
        var outsider = await UserAsync();
        var noPermission = await UserAsync(false);
        var body = new { noteCategoryId = f.CategoryId, text = "متن" };
        foreach (var token in new[] { outsider.Token, noPermission.Token })
        {
            foreach (var (method, url, data) in new (HttpMethod, string, object?)[]
            {
                (HttpMethod.Get, Notes(f.AssignmentId), null), (HttpMethod.Get, Note(Id(note)), null),
                (HttpMethod.Post, Notes(f.AssignmentId), body), (HttpMethod.Put, Note(Id(note)), body),
                (HttpMethod.Delete, Note(Id(note)), null), (HttpMethod.Post, Note(Id(note)) + "/seen", new { })
            })
                Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(method, url, data, bearer: token)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(HttpMethod.Get, Notes(f.AssignmentId))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Put, Note(Id(note)), body, bearer: f.TechnicianToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Delete, Note(Id(note)), bearer: f.TechnicianToken)).StatusCode);
        // Being Admin does not grant access to somebody else's assignment notes.
        var other = await FixtureAsync(sender: outsider.Id);
        Assert.Equal(HttpStatusCode.Forbidden, (await AdminSendAsync(HttpMethod.Get, Notes(other.AssignmentId))).StatusCode);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task Terminal_assignments_still_accept_notes(long status)
    {
        var f = await FixtureAsync(status);
        var note = await CreateAsync(f);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, Note(Id(note)), bearer: f.TechnicianToken)).StatusCode);
        await SeenAsync(f, note);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Empty_text_is_rejected_on_create_and_update(string? text)
    {
        var f = await FixtureAsync();
        var note = await CreateAsync(f);
        var body = new { noteCategoryId = f.CategoryId, text };
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Post, Notes(f.AssignmentId), body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Put, Note(Id(note)), body)).StatusCode);
    }

    [Fact]
    public async Task Text_is_bounded_and_category_must_exist_and_be_active()
    {
        var f = await FixtureAsync();
        await CreateAsync(f, new string('ی', 2000));
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Post, Notes(f.AssignmentId), new { noteCategoryId = f.CategoryId, text = new string('ی', 2001) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AdminSendAsync(HttpMethod.Post, Notes(f.AssignmentId), new { noteCategoryId = long.MaxValue, text = "متن" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Post, Notes(f.AssignmentId), new { noteCategoryId = 0, text = "متن" })).StatusCode);
        var category = await CatalogFixtures.PostAsync(this, "/api/NoteCategory", new { title = "غیرفعال " + Unique() }, "id");
        Assert.Equal(HttpStatusCode.OK, (await AdminSendAsync(HttpMethod.Delete, $"/api/NoteCategory/{category}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Post, Notes(f.AssignmentId), new { noteCategoryId = category, text = "متن" })).StatusCode);
    }

    [Fact]
    public async Task Soft_delete_is_author_only_and_categories_are_guarded_until_notes_are_deleted()
    {
        var f = await FixtureAsync();
        var note = await CreateAsync(f);
        Assert.Equal(HttpStatusCode.Conflict, (await AdminSendAsync(HttpMethod.Delete, $"/api/NoteCategory/{f.CategoryId}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AdminSendAsync(HttpMethod.Delete, Note(Id(note)))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await AdminSendAsync(HttpMethod.Delete, Note(Id(note)))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Post, Note(Id(note)) + "/seen", new { }, bearer: f.TechnicianToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AdminSendAsync(HttpMethod.Put, Note(Id(note)), new { noteCategoryId = f.CategoryId, text = "متن" })).StatusCode);
        var active = await ReadDataAsync(await AdminSendAsync(HttpMethod.Get, Notes(f.AssignmentId)));
        Assert.Empty(active.EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await AdminSendAsync(HttpMethod.Delete, $"/api/NoteCategory/{f.CategoryId}")).StatusCode);
        var historical = await ReadDataAsync(await AdminSendAsync(HttpMethod.Get, Notes(f.AssignmentId) + "?isActive=false"));
        Assert.False(Assert.Single(historical.EnumerateArray()).GetProperty("isActive").GetBoolean());
        Assert.False((await ReadDataAsync(await AdminSendAsync(HttpMethod.Get, Note(Id(note))))).GetProperty("isActive").GetBoolean());
    }

    [Fact]
    public async Task Paging_search_category_and_seen_filters_remain_within_one_assignment()
    {
        var f = await FixtureAsync();
        var first = await CreateAsync(f, "کلیدواژه اول");
        await CreateAsync(f, "کلیدواژه دوم");
        var other = await FixtureAsync();
        await CreateAsync(other, "کلیدواژه خارج");
        await SeenAsync(f, first);
        var response = await AdminSendAsync(HttpMethod.Get, Notes(f.AssignmentId) + $"?noteCategoryId={f.CategoryId}&search=کلیدواژه&isSeen=true&pageSize=1");
        var json = await ReadJsonAsync(response);
        Assert.Equal(1, PaginationOf(json).GetProperty("totalCount").GetInt32());
        Assert.Equal(Id(first), Id(Assert.Single(DataOf(json).EnumerateArray())));
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Get, Notes(f.AssignmentId) + "?pageSize=101")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AdminSendAsync(HttpMethod.Get, Notes(long.MaxValue))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Get, Notes(-1))).StatusCode);
    }

    [Fact]
    public async Task Editing_or_acknowledging_a_note_does_not_modify_its_assignment_or_category()
    {
        var f = await FixtureAsync();
        var note = await CreateAsync(f);
        async Task<(DateTime?, DateTime?)> Versions()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            return ((await db.TechnicianAssignments.SingleAsync(x => x.TechnicianAssignmentId == f.AssignmentId)).ModifiedDateTime,
                (await db.Set<NoteCategory>().SingleAsync(x => x.NoteCategoryId == f.CategoryId)).ModifiedDateTime);
        }
        var before = await Versions();
        var seen = await SeenAsync(f, note);
        await AdminSendAsync(HttpMethod.Put, Note(Id(note)), new { noteCategoryId = f.CategoryId, text = "متن تازه", rowVersion = Version(seen) });
        Assert.Equal(before, await Versions());
    }
}
