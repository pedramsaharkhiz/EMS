using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace EMS.Tests.Integration
{
    /// <summary>Rate limit of the sign-in and refresh endpoints, and the password work done for a user name that does not exist.</summary>
    [Collection("Api")]      // the shared API collection: tests that start extra hosts must not run beside tests that read the shared log sink
    public class LoginProtectionTests
    {
        private readonly EmsApiFactory _factory;
        public LoginProtectionTests(EmsApiFactory factory) => _factory = factory;

        private HttpClient LimitedClient(int loginLimit, int refreshLimit) =>
            _factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:RateLimiting:Enabled"] = "true",
                ["Security:RateLimiting:Login:PermitLimit"] = loginLimit.ToString(),
                ["Security:RateLimiting:Login:WindowSeconds"] = "60",
                ["Security:RateLimiting:Refresh:PermitLimit"] = refreshLimit.ToString(),
                ["Security:RateLimiting:Refresh:WindowSeconds"] = "60",
            }))).CreateClient();

        [Fact]
        public async Task Sign_in_is_refused_with_429_after_the_limit_and_the_answer_says_when_to_retry()
        {
            var client = LimitedClient(loginLimit: 3, refreshLimit: 30);
            for (var i = 0; i < 3; i++)
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/Identity/Login", new { userName = "nobody", password = "x" })).StatusCode);

            var blocked = await client.PostAsJsonAsync("/api/Identity/Login", new { userName = "admin", password = "whatever" });
            Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
            Assert.True(int.Parse(blocked.Headers.GetValues("Retry-After").Single()) is > 0 and <= 60);
            var body = await blocked.Content.ReadAsStringAsync();
            Assert.Contains("too_many_requests", body);
            Assert.Contains("\"success\":false", body.Replace(" ", ""));
            Assert.True(blocked.Headers.Contains("X-Content-Type-Options"));        // even a 429 carries the security headers
        }

        [Fact]
        public async Task Refresh_has_its_own_limit_and_other_endpoints_are_not_limited()
        {
            var client = LimitedClient(loginLimit: 100, refreshLimit: 2);
            for (var i = 0; i < 2; i++)
                Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/Identity/RefreshToken", new { refreshToken = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/Identity/RefreshToken", new { refreshToken = "x" })).StatusCode);

            // the login limit is separate (100 here), and a route without the attribute is never limited
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/Identity/Login", new { userName = "nobody", password = "x" })).StatusCode);
            for (var i = 0; i < 5; i++)
                Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.GetAsync("/health/live")).StatusCode);
        }

        [Fact]
        public async Task Nothing_is_limited_when_the_switch_is_off()
        {
            var client = _factory.CreateClient();           // the test host switches the limit off
            for (var i = 0; i < 15; i++)
                Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.PostAsJsonAsync("/api/Identity/Login", new { userName = "nobody", password = "x" })).StatusCode);
        }

        // ---- timing: an unknown user name costs the same password work as a wrong password

        private sealed class CountingHasher : PasswordHasher<ApplicationUser>
        {
            public static int Verifications;
            public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
            {
                Interlocked.Increment(ref Verifications);
                return base.VerifyHashedPassword(user, hashedPassword, providedPassword);
            }
        }

        [Fact]
        public async Task An_unknown_user_name_still_costs_a_password_check()
        {
            var client = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
                services.AddSingleton<IPasswordHasher<ApplicationUser>, CountingHasher>())).CreateClient();

            var before = CountingHasher.Verifications;
            var response = await client.PostAsJsonAsync("/api/Identity/Login", new { userName = "no_such_user_" + Guid.NewGuid().ToString("N")[..8], password = "Whatever1" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(before + 1, CountingHasher.Verifications);
        }
    }
}
