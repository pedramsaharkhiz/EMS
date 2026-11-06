using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace EMS.Presentation.Security
{
    /// <summary>
    /// Behind a reverse proxy (IIS, nginx, a load balancer) every request seems to come from the proxy. When this is on, the client address and the
    /// original scheme (http / https) are taken from the <c>X-Forwarded-*</c> headers, but ONLY when the request comes from a listed proxy: those
    /// headers can be forged by anyone who reaches the application directly. Bound from <c>Security:ForwardedHeaders</c>.
    /// Off by default: turn it on only when the application really sits behind a proxy, and list that proxy.
    /// </summary>
    public sealed class ForwardedHeadersSettings
    {
        public const string SectionName = "Security:ForwardedHeaders";

        public bool Enabled { get; set; }

        /// <summary>How many proxies in a row are trusted (the number of hops between the client and the application).</summary>
        public int ForwardLimit { get; set; } = 1;

        /// <summary>Addresses of the proxies, e.g. "10.0.0.5". Empty (with no networks): only the same machine (127.0.0.1 and ::1) is trusted.</summary>
        public List<string> KnownProxies { get; set; } = new();

        /// <summary>Address ranges of the proxies in CIDR form, e.g. "10.0.0.0/24".</summary>
        public List<string> KnownNetworks { get; set; } = new();

        /// <summary>Applies the settings. A malformed entry stops the start-up: silently trusting nobody (or everybody) would be worse.</summary>
        public void ApplyTo(ForwardedHeadersOptions options)
        {
            if (ForwardLimit < 1)
                throw new InvalidOperationException($"{SectionName}:ForwardLimit must be at least 1.");

            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = ForwardLimit;

            if (KnownProxies.Count == 0 && KnownNetworks.Count == 0)
                return;                                   // keep the framework default: loopback only

            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var proxy in KnownProxies)
            {
                if (!IPAddress.TryParse(proxy?.Trim(), out var address))
                    throw new InvalidOperationException($"{SectionName}:KnownProxies has an invalid address: '{proxy}'.");
                options.KnownProxies.Add(address);
            }
            foreach (var network in KnownNetworks)
            {
                if (!System.Net.IPNetwork.TryParse(network?.Trim(), out var parsed))
                    throw new InvalidOperationException($"{SectionName}:KnownNetworks has an invalid range: '{network}' (use CIDR, e.g. 10.0.0.0/24).");
                options.KnownIPNetworks.Add(parsed);
            }
        }
    }
}
