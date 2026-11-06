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
        // Test credentials: the administrator that the project's own IdentitySeeder creates from Seed:AdminPassword (set in TestEnvironment).
        protected const string AdminUserName = "admin";
        protected const string AdminPassword = EMS.Tests.Support.TestEnvironment.AdminPassword;
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

        /// <summary>The whole response body: the envelope { success, message, data, meta, error, traceId }.</summary>
        protected async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }

        /// <summary>The <c>data</c> member of a response (the resource, or the array of a list).</summary>
        protected async Task<JsonElement> ReadDataAsync(HttpResponseMessage response) => DataOf(await ReadJsonAsync(response));

        protected static JsonElement DataOf(JsonElement envelope) => envelope.GetProperty("data");

        /// <summary>The paging information of a list response: <c>meta.pagination</c>.</summary>
        protected static JsonElement PaginationOf(JsonElement envelope) => envelope.GetProperty("meta").GetProperty("pagination");

        /// <summary>The stable <c>error.code</c> of an error response.</summary>
        protected static string? ErrorCodeOf(JsonElement envelope) => envelope.GetProperty("error").GetProperty("code").GetString();

        /// <summary>The <c>error.details</c> field names of a validation error.</summary>
        protected static List<string> ErrorFieldsOf(JsonElement envelope) =>
            envelope.GetProperty("error").GetProperty("details").EnumerateArray().Select(d => d.GetProperty("field").GetString()!).ToList();

        protected async Task<(string AccessToken, string RefreshToken, long UserId)> LoginAsync(string userName, string password, string? correlationId = null)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName, password }, correlationId);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var json = await ReadDataAsync(response);
            return (json.GetProperty("accessToken").GetString()!, json.GetProperty("refreshToken").GetString()!, json.GetProperty("userId").GetInt64());
        }

        protected async Task<(string AccessToken, string RefreshToken, long UserId)> LoginAdminAsync() =>
            await LoginAsync(AdminUserName, AdminPassword);

        protected async Task<long> CreateUserAsync(string adminToken, string userName, string password, long? postId = null)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Users", new
            {
                userName,
                password,
                firstName = "Test",
                lastName = "User",
                personnelCode = "PC-" + userName,
                postId
            }, bearer: adminToken);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            return (await ReadDataAsync(response)).GetProperty("id").GetInt64();
        }

        /// <summary>Creates a role holding exactly the given permissions and returns its id.</summary>
        protected async Task<long> CreateRoleAsync(string adminToken, string roleName, params string[] permissions)
        {
            var response = await SendAsync(HttpMethod.Post, "/api/Role", new { name = roleName }, bearer: adminToken);
            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            var roleId = (await ReadDataAsync(response)).GetProperty("id").GetInt64();

            var assign = await SendAsync(HttpMethod.Post, "/api/Role/AssignPermissions", new { roleName, permissions }, bearer: adminToken);
            Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
            return roleId;
        }

        protected async Task AssignRoleAsync(string adminToken, long userId, long roleId) =>
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Post, $"/api/Users/{userId}/roles/{roleId}", bearer: adminToken)).StatusCode);

        protected async Task RemoveRoleAsync(string adminToken, long userId, long roleId) =>
            Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/Users/{userId}/roles/{roleId}", bearer: adminToken)).StatusCode);

        // Entry points for fixture helpers that live outside the test class hierarchy.
        public IServiceProvider ServicesForFixtures => _factory.Services;
        public Task<(string AccessToken, string RefreshToken, long UserId)> LoginAdminForFixturesAsync() => LoginAdminAsync();
        public Task<HttpResponseMessage> SendForFixturesAsync(HttpMethod method, string url, object? body, string bearer) => SendAsync(method, url, body, bearer: bearer);
        public Task<JsonElement> ReadForFixturesAsync(HttpResponseMessage response) => ReadDataAsync(response);

        protected async Task<HttpStatusCode> StatusOfAsync(HttpMethod method, string url, string bearer, object? body = null) =>
            (await SendAsync(method, url, body, bearer: bearer)).StatusCode;
    }
}
