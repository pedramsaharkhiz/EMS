using EMS.Tests.Support;
using Serilog.Events;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EMS.Tests.Integration
{
    /// <summary>All integration tests share one running API (xUnit collection) so the app boots only once.</summary>
    [CollectionDefinition("Api")]
    public class ApiCollection : ICollectionFixture<EmsApiFactory> { }

    [Collection("Api")]
    public abstract class ApiTestBase
    {
        // Test credentials: the default administrator that the project's own IdentitySeeder creates.
        protected const string AdminUserName = "admin";
        protected const string AdminPassword = "Admin@123456";
        protected const string GenericLoginError = "نام کاربری یا کلمه عبور نامعتبر است.";

        protected readonly EmsApiFactory _factory;
        protected readonly HttpClient _client;
        private static int _counter;

        protected ApiTestBase(EmsApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        protected CapturingSink Sink => _factory.Sink;

        protected static string NewCorrelationId(string prefix) =>
            $"{prefix}-{Interlocked.Increment(ref _counter):D4}-{Guid.NewGuid():N}"[..40];

        protected static string Rendered(LogEvent e) => e.RenderMessage();

        protected async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, string? correlationId = null, string? bearer = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body);
            if (correlationId is not null)
                request.Headers.Add("X-Correlation-ID", correlationId);
            if (bearer is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _client.SendAsync(request);
        }

        protected async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        protected async Task<(string AccessToken, string RefreshToken, long UserId)> LoginAsync(string userName, string password, string? correlationId = null)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName, password }, correlationId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadJsonAsync(response);
            return (json.GetProperty("accessToken").GetString()!, json.GetProperty("refreshToken").GetString()!, json.GetProperty("userId").GetInt64());
        }

        protected async Task<(string AccessToken, string RefreshToken, long UserId)> LoginAdminAsync() =>
            await LoginAsync(AdminUserName, AdminPassword);

        protected async Task<long> CreateUserAsync(string adminToken, string userName, string password)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Users", new
            {
                userName,
                password,
                firstName = "Test",
                lastName = "User",
                personnelCode = "PC-" + userName
            }, bearer: adminToken);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var json = await ReadJsonAsync(response);
            return json.TryGetProperty("userId", out var id) ? id.GetInt64() : json.GetProperty("id").GetInt64();
        }

        /// <summary>Creates a role holding exactly the given permissions and returns its id.</summary>
        protected async Task<long> CreateRoleAsync(string adminToken, string roleName, params string[] permissions)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Role", new { name = roleName }, bearer: adminToken);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var json = await ReadJsonAsync(response);
            var roleId = json.TryGetProperty("roleId", out var rid) ? rid.GetInt64() : json.GetProperty("id").GetInt64();

            var assign = await SendAsync(HttpMethod.Post, "/api/Role/AssignPermissions", new { roleName, permissions }, bearer: adminToken);
            Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
            return roleId;
        }

        protected async Task AssignRoleAsync(string adminToken, long userId, long roleId) =>
            Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Post, $"/api/Users/{userId}/roles/{roleId}", bearer: adminToken)).StatusCode);

        protected async Task RemoveRoleAsync(string adminToken, long userId, long roleId) =>
            Assert.Equal(HttpStatusCode.NoContent, (await SendAsync(HttpMethod.Delete, $"/api/Users/{userId}/roles/{roleId}", bearer: adminToken)).StatusCode);

        protected async Task<HttpStatusCode> StatusOfAsync(HttpMethod method, string url, string bearer, object? body = null) =>
            (await SendAsync(method, url, body, bearer: bearer)).StatusCode;
    }
}
