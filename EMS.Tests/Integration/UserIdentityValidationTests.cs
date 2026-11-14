using EMS.Application.Common.Security;
using EMS.Tests.Support;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration;

public class UserIdentityValidationTests(EmsApiFactory factory) : ApiTestBase(factory)
{
    protected override bool RolesSeeEveryDepartment => false;
    private string? _admin;
    private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
    private static Dictionary<string, object?> Body(string? national = null) => new()
    {
        ["userName"] = "ui_" + Guid.NewGuid().ToString("N")[..12], ["password"] = "Passw0rd!x",
        ["firstName"] = "آزمایش", ["lastName"] = "کاربر", ["personnelCode"] = "PC-" + Guid.NewGuid().ToString("N")[..20],
        ["nationalCode"] = national
    };
    private async Task<HttpResponseMessage> SendUserAsync(Dictionary<string, object?> body, long? id = null, string? token = null) =>
        await SendAsync(id is null ? HttpMethod.Post : HttpMethod.Put, "/api/Users" + (id is null ? "" : "/" + id), body, bearer: token ?? await AdminAsync());
    private async Task<long> CreateAsync(Dictionary<string, object?> body)
    {
        var r = await SendUserAsync(body);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await ReadDataAsync(r)).GetProperty("id").GetInt64();
    }
    private async Task AssertConflictAsync(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await ReadJsonAsync(response);
        Assert.Equal(field + "_taken", ErrorCodeOf(error));
        Assert.Contains(field == "personnel_code" ? "کد پرسنلی" : "کد ملی", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Create_and_update_normalize_identifiers_and_keep_unchanged_values()
    {
        var code = NationalCodeFixture.New();
        var body = Body(" " + string.Concat(code.Select(c => (char)('۰' + c - '0'))) + " ");
        body["personnelCode"] = "  A-۱۲٣-" + Guid.NewGuid().ToString("N")[..8] + "  ";
        var id = await CreateAsync(body);
        var update = await SendUserAsync(body, id);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var detail = await ReadDataAsync(update);
        Assert.Equal(code, detail.GetProperty("nationalCode").GetString());
        Assert.StartsWith("A-123-", detail.GetProperty("personnelCode").GetString());

        body["nationalCode"] = " \t ";
        Assert.Equal(HttpStatusCode.OK, (await SendUserAsync(body, id)).StatusCode);
        var get = await SendAsync(HttpMethod.Get, $"/api/Users/{id}", bearer: await AdminAsync());
        Assert.Equal(JsonValueKind.Null, (await ReadDataAsync(get)).GetProperty("nationalCode").ValueKind);
        await CreateAsync(Body()); // several users may omit national code
        await CreateAsync(Body());
    }

    [Fact]
    public async Task Duplicate_identifiers_of_inactive_users_are_rejected_on_create_and_update()
    {
        var original = Body(NationalCodeFixture.New()); original["isActive"] = false;
        await CreateAsync(original);
        var second = Body(NationalCodeFixture.New()); var id = await CreateAsync(second);
        foreach (var field in new[] { "personnelCode", "nationalCode" })
        {
            var duplicate = Body(); duplicate[field] = " " + original[field] + " ";
            await AssertConflictAsync(await SendUserAsync(duplicate), field == "personnelCode" ? "personnel_code" : "national_code");
            var edit = new Dictionary<string, object?>(second) { [field] = original[field] };
            await AssertConflictAsync(await SendUserAsync(edit, id), field == "personnelCode" ? "personnel_code" : "national_code");
        }
    }

    [Fact]
    public async Task Hidden_duplicates_are_still_detected_without_leaking_user_details()
    {
        var original = Body(NationalCodeFixture.New()); await CreateAsync(original);
        var name = "scope_" + Guid.NewGuid().ToString("N")[..10];
        var user = await CreateUserAsync(await AdminAsync(), name, "Passw0rd!x");
        // Intentionally no DataScope.ViewAll and no department membership.
        var role = await CreateRoleAsync(await AdminAsync(), "identity_" + name, Permissions.Users.Create);
        await AssignRoleAsync(await AdminAsync(), user, role);
        var token = (await LoginAsync(name, "Passw0rd!x")).AccessToken;
        var duplicate = Body(original["nationalCode"]!.ToString());
        await AssertConflictAsync(await SendUserAsync(duplicate, token: token), "national_code");
    }

    [Theory]
    [InlineData("123", "کد ملی باید دقیقاً ۱۰ رقم باشد.")]
    [InlineData("12345678901", "کد ملی باید دقیقاً ۱۰ رقم باشد.")]
    [InlineData("123456789a", "کد ملی باید دقیقاً ۱۰ رقم باشد.")]
    [InlineData("1111111111", "کد ملی واردشده معتبر نیست؛ لطفاً آن را بررسی کنید.")]
    [InlineData("0012345678", "کد ملی واردشده معتبر نیست؛ لطفاً آن را بررسی کنید.")]
    public async Task Invalid_national_codes_return_readable_field_errors_on_create_and_update(string value, string message)
    {
        var body = Body(); var id = await CreateAsync(body);
        body["nationalCode"] = value;
        foreach (var target in new long?[] { null, id })
        {
            var response = await SendUserAsync(body, target);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var json = await ReadJsonAsync(response);
            Assert.Equal("validation_failed", ErrorCodeOf(json));
            Assert.Contains(json.GetProperty("error").GetProperty("details").EnumerateArray(),
                e => e.GetProperty("field").GetString() == "nationalCode" && e.GetProperty("message").GetString() == message);
        }
    }
}
