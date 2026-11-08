namespace EMS.Presentation.Security
{
    /// <summary>
    /// The browser origins allowed to call the API (CORS). Bound from configuration section <c>Cors</c>.
    /// An origin is the address the browser shows for the page: scheme + host (+ port when it is not the default 80/443),
    /// with no path and no trailing slash: <c>http://192.168.200.62</c>, <c>https://app.example.com</c>, <c>http://localhost:5173</c>.
    /// The browser never sends the default port, so <c>http://host:80</c> would never match.
    /// </summary>
    public sealed class CorsSettings
    {
        public const string SectionName = "Cors";

        public List<string> AllowedOrigins { get; set; } = new();

        /// <summary>null when valid, otherwise the first problem found.</summary>
        public string? Validate()
        {
            foreach (var origin in AllowedOrigins)
            {
                // credentials are allowed, so "*" is not a valid origin (and would be refused by browsers anyway)
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                    || origin.EndsWith('/')
                    || !string.IsNullOrEmpty(uri.PathAndQuery.Trim('/'))
                    || !string.IsNullOrEmpty(uri.Fragment)
                    || (uri.IsDefaultPort && System.Text.RegularExpressions.Regex.IsMatch(origin, @":\d+$")))
                    return $"Cors:AllowedOrigins contains an invalid origin '{origin}' (use scheme://host[:port], no path, no trailing slash, no default port).";
            }
            return null;
        }
    }

    /// <summary>Redirecting http requests to https. Bound from configuration section <c>Https</c>.</summary>
    public sealed class HttpsOptions
    {
        public const string SectionName = "Https";

        /// <summary>
        /// When true, http requests are redirected to https (307) once the app knows an https port. A pre-flight cannot follow a redirect,
        /// so turn it off while the UI and the API are served over http.
        /// </summary>
        public bool RedirectEnabled { get; set; } = true;
    }
}
