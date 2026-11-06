using EMS.Infrastructure.Options;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Builder;
using System.Net;
using Xunit;

namespace EMS.Tests.Unit
{
    public class SecretsAndSecurityConfigTests
    {
        private static JwtOptions Valid() => new()
        {
            Issuer = "i", Audience = "a", SecretKey = new string('k', 40), AccessTokenExpirationMinutes = 15, RefreshTokenExpirationDays = 7,
        };

        [Fact]
        public void A_strong_random_key_is_accepted() => Assert.Null(JwtOptionsValidator.Validate(Valid()));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("too-short-key")]
        [InlineData("THIS_IS_A_VERY_STRONG_SECRET_KEY_FOR_EMS_2026")]      // the value that was published in the repository
        public void A_missing_short_or_published_key_is_refused(string? key)
        {
            var options = Valid();
            options.SecretKey = key!;
            Assert.NotNull(JwtOptionsValidator.Validate(options));
        }

        [Fact]
        public void The_repository_holds_no_secret()
        {
            var root = AppContext.BaseDirectory;
            while (root is not null && !File.Exists(Path.Combine(root, "EMS.slnx")))
                root = Path.GetDirectoryName(root);
            Assert.NotNull(root);

            foreach (var file in new[] { "appsettings.json", "appsettings.Development.json", "appsettings.Production.json" })
            {
                var text = File.ReadAllText(Path.Combine(root!, "EMS.Presentation", file));
                Assert.DoesNotContain("SecretKey", text);
                Assert.DoesNotContain("ConnectionStrings", text);
                Assert.DoesNotContain("password=", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("Admin@123456", text);
            }
        }

        [Fact]
        public void Forwarded_headers_with_no_list_keep_the_default_of_trusting_only_the_same_machine()
        {
            var options = new ForwardedHeadersOptions();
            new ForwardedHeadersSettings { Enabled = true }.ApplyTo(options);
            Assert.True(options.KnownProxies.Count + options.KnownIPNetworks.Count > 0);        // the framework's loopback default is left in place
            Assert.All(options.KnownProxies, p => Assert.True(IPAddress.IsLoopback(p)));
        }

        [Fact]
        public void Forwarded_headers_trust_exactly_the_listed_proxies_and_networks()
        {
            var options = new ForwardedHeadersOptions();
            new ForwardedHeadersSettings { Enabled = true, ForwardLimit = 2, KnownProxies = { "10.0.0.5" }, KnownNetworks = { "192.168.10.0/24" } }.ApplyTo(options);
            Assert.Equal(new[] { IPAddress.Parse("10.0.0.5") }, options.KnownProxies);
            Assert.Single(options.KnownIPNetworks);
            Assert.Equal(2, options.ForwardLimit);
        }

        [Theory]
        [InlineData("not-an-ip", null, 1)]
        [InlineData(null, "10.0.0.0", 1)]               // a range needs its /prefix
        [InlineData(null, null, 0)]                      // the number of hops must be at least 1
        public void A_malformed_forwarded_headers_setting_stops_the_start_up(string? proxy, string? network, int limit)
        {
            var settings = new ForwardedHeadersSettings { Enabled = true, ForwardLimit = limit };
            if (proxy is not null) settings.KnownProxies.Add(proxy);
            if (network is not null) settings.KnownNetworks.Add(network);
            Assert.Throws<InvalidOperationException>(() => settings.ApplyTo(new ForwardedHeadersOptions()));
        }

        [Fact]
        public void An_IPv6_client_is_limited_by_its_64_bit_prefix_and_an_IPv4_mapped_one_like_IPv4()
        {
            Assert.Equal(RateLimitingExtensions.ClientKey(IPAddress.Parse("2001:db8:1:2:aaaa::1")), RateLimitingExtensions.ClientKey(IPAddress.Parse("2001:db8:1:2:bbbb::9")));
            Assert.NotEqual(RateLimitingExtensions.ClientKey(IPAddress.Parse("2001:db8:1:2::1")), RateLimitingExtensions.ClientKey(IPAddress.Parse("2001:db8:1:3::1")));
            Assert.Equal("10.1.2.3", RateLimitingExtensions.ClientKey(IPAddress.Parse("::ffff:10.1.2.3")));
            Assert.Equal("unknown", RateLimitingExtensions.ClientKey(null));
        }
    }
}
