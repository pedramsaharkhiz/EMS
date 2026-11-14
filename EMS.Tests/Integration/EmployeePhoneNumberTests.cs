using EMS.Application.Common.Security;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace EMS.Tests.Integration;

public class EmployeePhoneNumberTests(EmsApiFactory factory) : ApiTestBase(factory)
{
    protected override bool RolesSeeEveryDepartment => false;
    private string? _admin;
    private async Task<string> AdminAsync() => _admin ??= (await LoginAdminAsync()).AccessToken;
    private static int _next = Random.Shared.Next(10000000, 80000000);
    public static string Number() => "091" + Interlocked.Increment(ref _next);
    private static long Id(JsonElement item) => item.GetProperty("id").GetInt64();
    private static string Version(JsonElement item) => item.GetProperty("rowVersion").GetString()!;
    private async Task<HttpResponseMessage> AdminSendAsync(HttpMethod method, string url, object? body = null) =>
        await SendAsync(method, url, body, bearer: await AdminAsync());
    private async Task<long> EmployeeAsync(long? department = null)
    {
        using var scope = _factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
        var employee = new Employee
        {
            FirstName = "کارمند", LastName = "بانک", PersonnelCode = Guid.NewGuid().ToString("N"), NationalCode = "0012345679",
            BirthDate = new DateTime(1990, 1, 1), OrganizationStructure = new OrganizationStructure { Title = "ساختار" },
            Organization = new Organization { Title = "شعبه", SupportingDepartmentId = department, OrganizationType = new OrganizationType { Title = "نوع" } }
        };
        db.Employees.Add(employee); await db.SaveChangesAsync(); return employee.EmployeeId;
    }
    private async Task<JsonElement> CreateAsync(long employee, string number)
    {
        var response = await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber", new { employeeId = employee, phoneNumber = number });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadDataAsync(response);
    }
    [Fact]
    public async Task Crud_normalizes_lists_updates_deactivates_and_physically_deletes_freeing_numbers()
    {
        var employee = await EmployeeAsync(); var number = Number();
        var first = await CreateAsync(employee, " +98" + number[1..] + " ");
        Assert.Equal(number, first.GetProperty("phoneNumber").GetString());
        Assert.Equal(employee, first.GetProperty("employeeId").GetInt64());
        Assert.True(first.GetProperty("isActive").GetBoolean());
        var secondNumber = Number(); var second = await CreateAsync(employee, secondNumber);
        var list = await ReadDataAsync(await AdminSendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber?employeeId={employee}&sortDirection=desc"));
        Assert.Equal(new[] { Id(second), Id(first) }, list.EnumerateArray().Select(Id));
        var replacement = Number();
        var edit = await AdminSendAsync(HttpMethod.Put, $"/api/EmployeePhoneNumber/{Id(first)}", new { phoneNumber = replacement, isActive = false, rowVersion = Version(first) });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        var inactive = await ReadDataAsync(edit);
        Assert.False(inactive.GetProperty("isActive").GetBoolean());
        var other = await EmployeeAsync(); await CreateAsync(other, number); // old number is free after editing
        var conflict = await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber", new { employeeId = other, phoneNumber = replacement });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode); // inactive rows still reserve their number
        var filtered = await ReadDataAsync(await AdminSendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber?employeeId={employee}&isActive=false&search={replacement}"));
        Assert.Equal(Id(first), Id(Assert.Single(filtered.EnumerateArray())));
        Assert.Equal(HttpStatusCode.OK, (await AdminSendAsync(HttpMethod.Delete, $"/api/EmployeePhoneNumber/{Id(first)}?rowVersion={Version(inactive)}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AdminSendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber/{Id(first)}")).StatusCode);
        await CreateAsync(other, replacement);
        using var scope = _factory.Services.CreateScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<EMSDbContext>().EmployeePhoneNumbers.IgnoreQueryFilters().AnyAsync(p => p.EmployeePhoneNumberId == Id(first)));
    }
    [Fact]
    public async Task Bulk_delete_is_atomic_reports_bad_item_and_releases_numbers()
    {
        var employee = await EmployeeAsync(); var a = await CreateAsync(employee, Number()); var b = await CreateAsync(employee, Number());
        var bad = await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber/bulk-delete", new { items = new[] { new { id = Id(a), rowVersion = Version(a) }, new { id = long.MaxValue, rowVersion = "0" } } });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Contains("items[1].id", ErrorFieldsOf(await ReadJsonAsync(bad)));
        Assert.Equal(HttpStatusCode.OK, (await AdminSendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber/{Id(a)}")).StatusCode);
        var edit = await AdminSendAsync(HttpMethod.Put, $"/api/EmployeePhoneNumber/{Id(b)}", new { phoneNumber = b.GetProperty("phoneNumber").GetString(), isActive = false });
        var current = await ReadDataAsync(edit);
        var stale = await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber/bulk-delete", new { items = new[] { new { id = Id(a), rowVersion = Version(a) }, new { id = Id(b), rowVersion = Version(b) } } });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("items[1].rowVersion", ErrorFieldsOf(await ReadJsonAsync(stale)));
        var good = await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber/bulk-delete", new { items = new[] { new { id = Id(a), rowVersion = Version(a) }, new { id = Id(b), rowVersion = Version(current) } } });
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AdminSendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber/{Id(a)}")).StatusCode);
        await CreateAsync(await EmployeeAsync(), a.GetProperty("phoneNumber").GetString()!);
    }
    [Theory]
    [InlineData("")]
    [InlineData("02112345678")]
    [InlineData("0912345678")]
    [InlineData("091234567890")]
    [InlineData("09123abc789")]
    public async Task Invalid_mobile_returns_a_readable_field_error(string number)
    {
        var response = await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber", new { employeeId = await EmployeeAsync(), phoneNumber = number });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Contains("phoneNumber", ErrorFieldsOf(json));
        Assert.Contains("شماره موبایل معتبر نیست", json.GetProperty("error").GetProperty("details")[0].GetProperty("message").GetString());
    }
    [Fact]
    public async Task Missing_and_duplicate_ids_or_null_bulk_items_are_rejected()
    {
        foreach (var body in new object[] { new { items = Array.Empty<object>() }, new { items = new object?[] { null } }, new { items = new[] { new { id = 1 }, new { id = 1 } } } })
            Assert.Equal(HttpStatusCode.BadRequest, (await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber/bulk-delete", body)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await AdminSendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber", new { employeeId = long.MaxValue, phoneNumber = Number() })).StatusCode);
    }
    [Fact]
    public async Task Permissions_and_employee_branch_scope_protect_every_path()
    {
        var a = await CatalogFixtures.NewDepartmentAsync(this); var b = await CatalogFixtures.NewDepartmentAsync(this);
        var visibleEmployee = await EmployeeAsync(a); var hiddenEmployee = await EmployeeAsync(b);
        var visible = await CreateAsync(visibleEmployee, Number()); var hidden = await CreateAsync(hiddenEmployee, Number());
        var name = "phone_" + Guid.NewGuid().ToString("N")[..10];
        var userId = await CreateUserAsync(await AdminAsync(), name, "Passw0rd!x");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EMSDbContext>();
            db.UserSupportingDepartments.Add(new UserSupportingDepartment { UserId = userId, SupportingDepartmentId = a }); await db.SaveChangesAsync();
        }
        var role = await CreateRoleAsync(await AdminAsync(), name, Permissions.EmployeePhoneNumbers.View, Permissions.EmployeePhoneNumbers.Create, Permissions.EmployeePhoneNumbers.Update, Permissions.EmployeePhoneNumbers.Delete);
        await AssignRoleAsync(await AdminAsync(), userId, role);
        var token = (await LoginAsync(name, "Passw0rd!x")).AccessToken;
        var list = await ReadDataAsync(await SendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber?employeeId={hiddenEmployee}", bearer: token));
        Assert.Empty(list.EnumerateArray());
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber/{Id(hidden)}", bearer: token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Put, $"/api/EmployeePhoneNumber/{Id(hidden)}", new { phoneNumber = Number() }, bearer: token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Delete, $"/api/EmployeePhoneNumber/{Id(hidden)}", bearer: token)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber", new { employeeId = hiddenEmployee, phoneNumber = Number() }, bearer: token)).StatusCode);
        var bulk = await SendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber/bulk-delete", new { items = new[] { new { id = Id(visible) }, new { id = Id(hidden) } } }, bearer: token);
        Assert.Equal(HttpStatusCode.BadRequest, bulk.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, $"/api/EmployeePhoneNumber/{Id(visible)}", bearer: token)).StatusCode);
        // Global uniqueness does not expose another employee's identity.
        var duplicate = await SendAsync(HttpMethod.Post, "/api/EmployeePhoneNumber", new { employeeId = visibleEmployee, phoneNumber = hidden.GetProperty("phoneNumber").GetString() }, bearer: token);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("employee_phone_number_taken", ErrorCodeOf(await ReadJsonAsync(duplicate)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/EmployeePhoneNumber")).StatusCode);
        var noRightsName = "none_" + Guid.NewGuid().ToString("N")[..10];
        await CreateUserAsync(await AdminAsync(), noRightsName, "Passw0rd!x");
        var noRights = (await LoginAsync(noRightsName, "Passw0rd!x")).AccessToken;
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAsync(HttpMethod.Get, "/api/EmployeePhoneNumber", bearer: noRights)).StatusCode);
    }
}
