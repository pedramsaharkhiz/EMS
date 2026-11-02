using EMS.Infrastructure.Options;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace EMS.Tests.Integration
{
    /// <summary>The real application with its real appsettings: headers on every kind of response, and cookies.</summary>
    public class SecurityConfigurationTests : ApiTestBase
    {
        public SecurityConfigurationTests(EmsApiFactory factory) : base(factory) { }

        private static string? Header(HttpResponseMessage r, string name) =>
            r.Headers.TryGetValues(name, out var v) ? string.Join(",", v)
            : r.Content.Headers.TryGetValues(name, out var c) ? string.Join(",", c) : null;

        private SecurityHeadersOptions Configured =>
            _factory.Services.GetRequiredService<IOptions<SecurityHeadersOptions>>().Value;

        private void AssertAllConfiguredHeadersArePresent(HttpResponseMessage response, string because)
        {
            var configured = Configured.Values.Where(kv => !string.IsNullOrWhiteSpace(kv.Value)).ToList();
            Assert.NotEmpty(configured);
            foreach (var (name, _) in configured)
                Assert.True(Header(response, name) is not null, $"{name} is missing on {because}");
        }

        // ------------------------------------------------------------------------------- headers

        [Fact]
        public void The_baseline_headers_are_present_in_appsettings_with_safe_values()
        {
            var values = Configured.Values;
            Assert.Equal("nosniff", values["X-Content-Type-Options"]);
            Assert.Equal("DENY", values["X-Frame-Options"]);
            Assert.False(string.IsNullOrWhiteSpace(values["Referrer-Policy"]));
            Assert.Contains("frame-ancestors 'none'", values["Content-Security-Policy"]);
            Assert.Contains("default-src 'none'", values["Content-Security-Policy"]);
            Assert.False(string.IsNullOrWhiteSpace(values["Permissions-Policy"]));
            Assert.False(string.IsNullOrWhiteSpace(values["Cross-Origin-Opener-Policy"]));
            Assert.False(string.IsNullOrWhiteSpace(values["Cross-Origin-Resource-Policy"]));
            Assert.Contains("no-store", values["Cache-Control"]);
            Assert.True(Configured.Hsts.Enabled);
            Assert.True(Configured.Hsts.MaxAgeDays >= 180);
        }

        [Fact]
        public async Task Every_configured_header_is_on_success_error_unknown_route_and_preflight_responses()
        {
            var ok = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = AdminPassword });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            AssertAllConfiguredHeadersArePresent(ok, "a 200 login");

            var unauthorized = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = "nobody", password = "Wrong1pass" });
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
            AssertAllConfiguredHeadersArePresent(unauthorized, "a 401");

            var anonymous = await SendAsync(HttpMethod.Get, "/api/Users");
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            AssertAllConfiguredHeadersArePresent(anonymous, "an unauthenticated request");

            var notFound = await SendAsync(HttpMethod.Get, "/api/this-route-does-not-exist");
            Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
            AssertAllConfiguredHeadersArePresent(notFound, "a 404");

            var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/Users");
            preflight.Headers.Add("Origin", "http://localhost:3000");
            preflight.Headers.Add("Access-Control-Request-Method", "GET");
            AssertAllConfiguredHeadersArePresent(await _client.SendAsync(preflight), "a CORS pre-flight");
        }

        [Fact]
        public async Task Responses_with_tokens_are_not_cacheable_and_framework_headers_are_not_overwritten()
        {
            var login = await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = AdminPassword });
            Assert.Contains("no-store", Header(login, "Cache-Control"));

            // The health endpoint sets its own Cache-Control: it must be left alone.
            var health = await SendAsync(HttpMethod.Get, "/health/live");
            Assert.Contains("no-cache", Header(health, "Cache-Control"));
        }

        [Fact]
        public async Task Technology_disclosure_headers_are_absent()
        {
            var response = await SendAsync(HttpMethod.Get, "/health/live");
            Assert.Null(Header(response, "X-Powered-By"));
        }

        [Fact]
        public async Task Hsts_is_sent_over_https_but_not_over_plain_http_or_localhost()
        {
            using var https = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.example.test") });
            var secure = await https.GetAsync("/health/live");
            Assert.Equal($"max-age={TimeSpan.FromDays(Configured.Hsts.MaxAgeDays).TotalSeconds:0}; includeSubDomains",
                Header(secure, "Strict-Transport-Security"));

            using var local = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            Assert.Null(Header(await local.GetAsync("/health/live"), "Strict-Transport-Security"));

            Assert.Null(Header(await SendAsync(HttpMethod.Get, "/health/live"), "Strict-Transport-Security")); // http://localhost
        }

        // ------------------------------------------------------------------------------- cookies

        [Fact]
        public async Task No_endpoint_sets_a_cookie()
        {
            var login = await LoginAdminAsync();
            var responses = new List<HttpResponseMessage>
            {
                await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = AdminPassword }),
                await SendAsync(HttpMethod.Post, "/api/Identity/Login", new { userName = AdminUserName, password = "Wrong1pass" }),
                await SendAsync(HttpMethod.Post, "/api/Identity/RefreshToken", new { refreshToken = login.RefreshToken }),
                await SendAsync(HttpMethod.Get, "/api/Users", bearer: login.AccessToken),
                await SendAsync(HttpMethod.Get, "/api/Users"),
                await SendAsync(HttpMethod.Get, "/health/ready"),
            };

            Assert.All(responses, r => Assert.False(r.Headers.Contains("Set-Cookie"), $"{r.RequestMessage?.RequestUri} set a cookie"));
        }

        [Fact]
        public void The_cookie_settings_come_from_appsettings_and_are_applied_to_every_identity_cookie()
        {
            var configured = _factory.Services.GetRequiredService<IOptions<CookieSecurityOptions>>().Value;
            Assert.True(configured.HttpOnly);
            Assert.Equal(CookieSecurePolicy.Always, configured.SecurePolicy);
            Assert.Equal(SameSiteMode.Strict, configured.SameSite);

            var monitor = _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
            foreach (var scheme in new[]
            {
                IdentityConstants.ApplicationScheme, IdentityConstants.ExternalScheme,
                IdentityConstants.TwoFactorRememberMeScheme, IdentityConstants.TwoFactorUserIdScheme
            })
            {
                var cookie = monitor.Get(scheme).Cookie;
                Assert.True(cookie.HttpOnly, $"{scheme}: HttpOnly");
                Assert.Equal(CookieSecurePolicy.Always, cookie.SecurePolicy);
                Assert.Equal(SameSiteMode.Strict, cookie.SameSite);
            }

            var application = monitor.Get(IdentityConstants.ApplicationScheme);
            Assert.Equal(TimeSpan.FromMinutes(configured.ExpireMinutes), application.ExpireTimeSpan);
            Assert.Equal(configured.SlidingExpiration, application.SlidingExpiration);
            Assert.False(application.SlidingExpiration);
        }

        [Fact]
        public async Task The_application_cookie_answers_401_and_403_instead_of_redirecting_to_a_login_page()
        {
            var options = _factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ApplicationScheme);
            var scheme = new AuthenticationScheme(IdentityConstants.ApplicationScheme, null, typeof(CookieAuthenticationHandler));

            var loginContext = new DefaultHttpContext();
            await options.Events.OnRedirectToLogin(new RedirectContext<CookieAuthenticationOptions>(loginContext, scheme, options, new AuthenticationProperties(), "/Account/Login"));
            Assert.Equal(401, loginContext.Response.StatusCode);

            var deniedContext = new DefaultHttpContext();
            await options.Events.OnRedirectToAccessDenied(new RedirectContext<CookieAuthenticationOptions>(deniedContext, scheme, options, new AuthenticationProperties(), "/Account/AccessDenied"));
            Assert.Equal(403, deniedContext.Response.StatusCode);
        }
    }
}
