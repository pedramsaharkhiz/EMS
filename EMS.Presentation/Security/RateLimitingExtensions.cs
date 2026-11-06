using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Presentation.Errors;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;

namespace EMS.Presentation.Security
{
    /// <summary>
    /// Limits for the two endpoints that need no token (sign in and refresh), per client address. Bound from <c>Security:RateLimiting</c>.
    /// The values are read when a request arrives, so a change of configuration needs no code change.
    /// </summary>
    public sealed class RateLimitingOptions
    {
        public const string SectionName = "Security:RateLimiting";

        /// <summary>Master switch. When false nothing is limited.</summary>
        public bool Enabled { get; set; } = true;

        public PolicySettings Login { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
        public PolicySettings Refresh { get; set; } = new() { PermitLimit = 30, WindowSeconds = 60 };

        public sealed class PolicySettings
        {
            /// <summary>Requests one client address may send in one window.</summary>
            public int PermitLimit { get; set; }
            public int WindowSeconds { get; set; }
        }

        /// <summary>The reason the options are not acceptable, or null.</summary>
        public string? Validate() =>
            Login.PermitLimit < 1 || Login.WindowSeconds < 1 || Refresh.PermitLimit < 1 || Refresh.WindowSeconds < 1
                ? $"{SectionName}: every PermitLimit and WindowSeconds must be at least 1."
                : null;
    }

    /// <summary>Names of the policies used by <c>[EnableRateLimiting]</c>.</summary>
    public static class RateLimitingPolicies
    {
        public const string Login = "login";
        public const string Refresh = "refresh";
    }

    public static class RateLimitingExtensions
    {
        public static IServiceCollection AddEmsRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<RateLimitingOptions>()
                .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
                .Validate(options => options.Validate() is null, "Security:RateLimiting configuration is invalid.")
                .ValidateOnStart();

            services.AddRateLimiter(limiter =>
            {
                limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                limiter.AddPolicy(RateLimitingPolicies.Login, context => Partition(context, RateLimitingPolicies.Login, o => o.Login));
                limiter.AddPolicy(RateLimitingPolicies.Refresh, context => Partition(context, RateLimitingPolicies.Refresh, o => o.Refresh));

                limiter.OnRejected = async (context, cancellationToken) =>
                {
                    var http = context.HttpContext;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        http.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

                    var policy = http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "unknown";
                    http.RequestServices.GetRequiredService<ISecurityEventLogger>().RateLimited(policy);

                    await ApiErrors.WriteAsync(http, StatusCodes.Status429TooManyRequests, ErrorCodes.TooManyRequests,
                        "تعداد درخواست‌های شما از حد مجاز گذشته است؛ کمی بعد دوباره تلاش کنید.");
                };
            });
            return services;
        }

        private static RateLimitPartition<string> Partition(HttpContext context, string policy, Func<RateLimitingOptions, RateLimitingOptions.PolicySettings> select)
        {
            var options = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
            if (!options.Enabled)
                return RateLimitPartition.GetNoLimiter($"{policy}:off");

            var settings = select(options);
            return RateLimitPartition.GetFixedWindowLimiter($"{policy}:{ClientKey(context.Connection.RemoteIpAddress)}", _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = Math.Max(1, settings.PermitLimit),
                Window = TimeSpan.FromSeconds(Math.Max(1, settings.WindowSeconds)),
                QueueLimit = 0,                    // over the limit: refuse at once, do not hold the request
                AutoReplenishment = true,
            });
        }

        /// <summary>
        /// One client = one address. An IPv6 user normally owns a whole /64 and can switch addresses inside it freely, so the first 64 bits are
        /// the key (otherwise the limit could be avoided by changing the last bits).
        /// </summary>
        public static string ClientKey(IPAddress? address)
        {
            if (address is null)
                return "unknown";
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            if (address.AddressFamily != AddressFamily.InterNetworkV6)
                return address.ToString();

            var bytes = address.GetAddressBytes();
            return new IPAddress(bytes.Take(8).Concat(new byte[8]).ToArray()) + "/64";
        }
    }
}
