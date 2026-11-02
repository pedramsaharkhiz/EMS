using EMS.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Net;

namespace EMS.Tests.Unit
{
    /// <summary>Runs the middleware in a real (in-memory) HTTP pipeline, because OnStarting callbacks only fire there.</summary>
    public class SecurityHeadersMiddlewareTests
    {
        private sealed class FixedMonitor : IOptionsMonitor<SecurityHeadersOptions>
        {
            public FixedMonitor(SecurityHeadersOptions value) => CurrentValue = value;
            public SecurityHeadersOptions CurrentValue { get; }
            public SecurityHeadersOptions Get(string? name) => CurrentValue;
            public IDisposable? OnChange(Action<SecurityHeadersOptions, string?> listener) => null;
        }

        private static async Task<(HttpResponseMessage Response, IHost Host)> SendAsync(
            SecurityHeadersOptions options,
            string url = "http://api.example.test/",
            Action<HttpContext>? app = null)
        {
            var host = await new HostBuilder().ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddLogging();
                    s.AddSingleton<IOptionsMonitor<SecurityHeadersOptions>>(new FixedMonitor(options));
                })
                .Configure(builder =>
                {
                    builder.UseMiddleware<SecurityHeadersMiddleware>();
                    builder.Run(async context =>
                    {
                        app?.Invoke(context);
                        await context.Response.WriteAsync("body");
                    });
                })).StartAsync();

            var client = host.GetTestClient();
            var response = await client.GetAsync(url);
            return (response, host);
        }

        private static SecurityHeadersOptions Defaults() => new()
        {
            Values =
            {
                ["X-Content-Type-Options"] = "nosniff",
                ["X-Frame-Options"] = "DENY",
                ["Content-Security-Policy"] = "default-src 'none'",
                ["Cache-Control"] = "no-store"
            },
            Remove = { "X-Powered-By" }
        };

        private static string? Header(HttpResponseMessage r, string name) =>
            r.Headers.TryGetValues(name, out var v) ? string.Join(",", v)
            : r.Content.Headers.TryGetValues(name, out var c) ? string.Join(",", c) : null;

        [Fact]
        public async Task Adds_every_configured_header_to_the_response()
        {
            var (response, host) = await SendAsync(Defaults());
            using var _ = host;

            Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
            Assert.Equal("DENY", Header(response, "X-Frame-Options"));
            Assert.Equal("default-src 'none'", Header(response, "Content-Security-Policy"));
            Assert.Equal("no-store", Header(response, "Cache-Control"));
        }

        [Fact]
        public async Task Applies_to_error_responses_too()
        {
            var (response, host) = await SendAsync(Defaults(), app: ctx => ctx.Response.StatusCode = 401);
            using var _ = host;

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        }

        [Fact]
        public async Task Never_overwrites_a_header_the_application_set_itself()
        {
            var (response, host) = await SendAsync(Defaults(), app: ctx => ctx.Response.Headers["Cache-Control"] = "no-cache, max-age=0");
            using var _ = host;

            Assert.Equal("no-cache, max-age=0", Header(response, "Cache-Control"));
        }

        [Fact]
        public async Task An_empty_value_switches_that_header_off()
        {
            var options = Defaults();
            options.Values["Content-Security-Policy"] = "";
            var (response, host) = await SendAsync(options);
            using var _ = host;

            Assert.Null(Header(response, "Content-Security-Policy"));
            Assert.Equal("nosniff", Header(response, "X-Content-Type-Options"));
        }

        [Fact]
        public async Task Any_header_can_be_added_from_configuration_and_unwanted_ones_removed()
        {
            var options = Defaults();
            options.Values["X-My-Custom-Header"] = "custom";
            var (response, host) = await SendAsync(options, app: ctx => ctx.Response.Headers["X-Powered-By"] = "something");
            using var _ = host;

            Assert.Equal("custom", Header(response, "X-My-Custom-Header"));
            Assert.Null(Header(response, "X-Powered-By"));
        }

        [Fact]
        public async Task Does_nothing_when_disabled()
        {
            var options = Defaults();
            options.Enabled = false;
            var (response, host) = await SendAsync(options, "https://api.example.test/", ctx => ctx.Response.Headers["X-Powered-By"] = "kept");
            using var _ = host;

            Assert.Null(Header(response, "X-Content-Type-Options"));
            Assert.Null(Header(response, "Strict-Transport-Security"));
            Assert.Equal("kept", Header(response, "X-Powered-By"));
        }

        // ------------------------------------------------------------------------------- HSTS

        [Fact]
        public async Task Hsts_is_sent_over_https_with_the_configured_lifetime()
        {
            var options = Defaults();
            options.Hsts.MaxAgeDays = 180;
            options.Hsts.IncludeSubDomains = true;
            options.Hsts.Preload = true;
            var (response, host) = await SendAsync(options, "https://api.example.test/");
            using var _ = host;

            Assert.Equal("max-age=15552000; includeSubDomains; preload", Header(response, "Strict-Transport-Security"));
        }

        [Theory]
        [InlineData("http://api.example.test/")]       // plain http: browsers ignore it, and it would be wrong to claim HTTPS
        [InlineData("https://localhost/")]             // never pin loopback to https
        [InlineData("https://127.0.0.1/")]
        public async Task Hsts_is_not_sent_over_http_or_to_loopback_hosts(string url)
        {
            var (response, host) = await SendAsync(Defaults(), url);
            using var _ = host;

            Assert.Null(Header(response, "Strict-Transport-Security"));
        }

        [Fact]
        public async Task Hsts_honours_extra_excluded_hosts_and_the_enabled_flag()
        {
            var excluded = Defaults();
            excluded.Hsts.ExcludedHosts.Add("internal.example.test");
            var (r1, h1) = await SendAsync(excluded, "https://internal.example.test/");
            using var _1 = h1;
            Assert.Null(Header(r1, "Strict-Transport-Security"));

            var off = Defaults();
            off.Hsts.Enabled = false;
            var (r2, h2) = await SendAsync(off, "https://api.example.test/");
            using var _2 = h2;
            Assert.Null(Header(r2, "Strict-Transport-Security"));
        }

        [Fact]
        public async Task Hsts_does_not_replace_a_value_set_by_the_application()
        {
            var (response, host) = await SendAsync(Defaults(), "https://api.example.test/",
                ctx => ctx.Response.Headers["Strict-Transport-Security"] = "max-age=1");
            using var _ = host;

            Assert.Equal("max-age=1", Header(response, "Strict-Transport-Security"));
        }

        // ------------------------------------------------------------------------------- validation

        [Fact]
        public void A_valid_configuration_passes_validation()
        {
            Assert.Null(Defaults().Validate());
        }

        [Theory]
        [InlineData("Bad Header Name", "x")]
        [InlineData("X-Evil", "value\r\nSet-Cookie: injected=1")]
        [InlineData("", "x")]
        [InlineData("X:Colon", "x")]
        public void An_invalid_header_name_or_value_is_rejected_at_start_up(string name, string value)
        {
            var options = new SecurityHeadersOptions { Values = { [name] = value } };
            Assert.NotNull(options.Validate());
        }

        [Fact]
        public void An_invalid_name_in_Remove_or_a_negative_hsts_lifetime_is_rejected()
        {
            Assert.NotNull(new SecurityHeadersOptions { Remove = { "bad name" } }.Validate());
            Assert.NotNull(new SecurityHeadersOptions { Hsts = { MaxAgeDays = -1 } }.Validate());
        }
    }
}
