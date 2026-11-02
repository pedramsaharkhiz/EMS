using Microsoft.Extensions.Options;

namespace EMS.Presentation.Security
{
    /// <summary>
    /// Adds the configured security headers to every response (including errors, 401/403 and CORS pre-flights),
    /// because it is the first middleware and applies the headers when the response starts.
    /// Reads <see cref="IOptionsMonitor{TOptions}"/>, so editing appsettings takes effect without a restart.
    /// </summary>
    public sealed class SecurityHeadersMiddleware
    {
        private const string StrictTransportSecurity = "Strict-Transport-Security";
        private static readonly string[] LoopbackHosts = { "localhost", "127.0.0.1", "[::1]", "::1" };

        private readonly RequestDelegate _next;
        private readonly IOptionsMonitor<SecurityHeadersOptions> _options;
        private readonly ILogger<SecurityHeadersMiddleware> _logger;

        public SecurityHeadersMiddleware(
            RequestDelegate next,
            IOptionsMonitor<SecurityHeadersOptions> options,
            ILogger<SecurityHeadersMiddleware> logger)
        {
            _next = next;
            _options = options;
            _logger = logger;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var options = _options.CurrentValue;
            if (options.Enabled)
            {
                context.Response.OnStarting(() =>
                {
                    Apply(context, options);
                    return Task.CompletedTask;
                });
            }

            return _next(context);
        }

        private void Apply(HttpContext context, SecurityHeadersOptions options)
        {
            try
            {
                var headers = context.Response.Headers;

                foreach (var name in options.Remove)
                    headers.Remove(name);

                foreach (var (name, value) in options.Values)
                {
                    if (string.IsNullOrWhiteSpace(value) || headers.ContainsKey(name))
                        continue;
                    headers[name] = value;
                }

                if (ShouldSendHsts(context, options) && !headers.ContainsKey(StrictTransportSecurity))
                    headers[StrictTransportSecurity] = BuildHsts(options.Hsts);
            }
            catch (Exception ex)
            {
                // A faulty header configuration must never turn a response into a failure.
                _logger.LogWarning(ex, "Could not apply the configured security headers to {Path}", context.Request.Path.Value);
            }
        }

        private static bool ShouldSendHsts(HttpContext context, SecurityHeadersOptions options)
        {
            if (!options.Hsts.Enabled || !context.Request.IsHttps)
                return false;

            var host = context.Request.Host.Host;
            return !LoopbackHosts.Contains(host, StringComparer.OrdinalIgnoreCase)
                   && !options.Hsts.ExcludedHosts.Contains(host, StringComparer.OrdinalIgnoreCase);
        }

        private static string BuildHsts(SecurityHeadersOptions.HstsSettings hsts)
        {
            var value = $"max-age={TimeSpan.FromDays(hsts.MaxAgeDays).TotalSeconds:0}";
            if (hsts.IncludeSubDomains)
                value += "; includeSubDomains";
            if (hsts.Preload)
                value += "; preload";
            return value;
        }
    }
}
