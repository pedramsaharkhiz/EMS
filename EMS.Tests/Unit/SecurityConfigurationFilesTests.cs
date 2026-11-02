using EMS.Infrastructure.Options;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EMS.Tests.Unit
{
    /// <summary>Loads the real appsettings files (as copied next to the tests) and checks what each environment ends up with.</summary>
    public class SecurityConfigurationFilesTests
    {
        private static IConfiguration Load(string? environment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false);
            if (environment is not null)
                builder.AddJsonFile($"appsettings.{environment}.json", optional: false);
            return builder.Build();
        }

        private static (SecurityHeadersOptions Headers, CookieSecurityOptions Cookies) Bind(IConfiguration config)
        {
            var headers = config.GetSection(SecurityHeadersOptions.SectionName).Get<SecurityHeadersOptions>()!;
            var cookies = config.GetSection(CookieSecurityOptions.SectionName).Get<CookieSecurityOptions>()!;
            return (headers, cookies);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Production")]
        public void Base_and_production_configuration_is_strict_and_valid(string? environment)
        {
            var (headers, cookies) = Bind(Load(environment));

            Assert.Null(headers.Validate());
            Assert.True(headers.Enabled);
            Assert.True(headers.RemoveServerHeader);
            Assert.True(headers.Hsts.Enabled);
            Assert.False(string.IsNullOrWhiteSpace(headers.Values["Content-Security-Policy"]));
            Assert.Contains("X-Powered-By", headers.Remove);

            Assert.True(cookies.HttpOnly);
            Assert.Equal(CookieSecurePolicy.Always, cookies.SecurePolicy);
            Assert.Equal(SameSiteMode.Strict, cookies.SameSite);
        }

        [Fact]
        public void Development_relaxes_only_what_local_http_development_needs()
        {
            var baseline = Bind(Load(null));
            var (headers, cookies) = Bind(Load("Development"));

            Assert.Null(headers.Validate());
            Assert.False(headers.Hsts.Enabled);                                   // localhost is plain http
            Assert.Equal("", headers.Values["Content-Security-Policy"]);          // the API documentation UI loads scripts
            Assert.Equal(CookieSecurePolicy.SameAsRequest, cookies.SecurePolicy); // cookies over http://localhost

            // Everything else is inherited unchanged from the base file.
            foreach (var (name, value) in baseline.Headers.Values.Where(kv => kv.Key != "Content-Security-Policy"))
                Assert.Equal(value, headers.Values[name]);
            Assert.Equal(baseline.Cookies.SameSite, cookies.SameSite);
            Assert.Equal(baseline.Cookies.HttpOnly, cookies.HttpOnly);
            Assert.True(headers.Enabled);
        }

        // ------------------------------------------------------------------------------- Server header on real Kestrel

        private static async Task<string?> ServerHeaderAsync(string? removeServerHeader)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            if (removeServerHeader is not null)
                builder.Configuration[$"{SecurityHeadersOptions.SectionName}:RemoveServerHeader"] = removeServerHeader;
            builder.WebHost.ConfigureServerHeaderFromConfiguration();

            await using var app = builder.Build();
            app.MapGet("/", () => "ok");
            await app.StartAsync();
            try
            {
                var address = app.Urls.Single();
                using var client = new HttpClient();
                var response = await client.GetAsync(address);
                return response.Headers.TryGetValues("Server", out var values) ? string.Join(",", values) : null;
            }
            finally
            {
                await app.StopAsync();
            }
        }

        [Fact]
        public async Task Kestrel_does_not_announce_itself_by_default()
        {
            Assert.Null(await ServerHeaderAsync(null));
            Assert.Null(await ServerHeaderAsync("true"));
        }

        [Fact]
        public async Task The_server_header_comes_back_when_the_setting_says_so()
        {
            Assert.Equal("Kestrel", await ServerHeaderAsync("false"));
        }
    }
}
