namespace EMS.Presentation.Security
{
    /// <summary>
    /// HTTP response security headers. Bound from configuration section <c>Security:Headers</c>
    /// (appsettings is the single source of truth for which headers are sent and with which values).
    /// </summary>
    public sealed class SecurityHeadersOptions
    {
        public const string SectionName = "Security:Headers";

        /// <summary>Master switch. When false no header is added or removed.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Do not send the "Server: Kestrel" header (applies to Kestrel only; IIS/reverse proxies add their own).</summary>
        public bool RemoveServerHeader { get; set; } = true;

        public HstsSettings Hsts { get; set; } = new();

        /// <summary>
        /// Header name -> value, added to every response that does not already carry that header
        /// (a header set by the application, e.g. Cache-Control on health checks, is never overwritten).
        /// Set a value to an empty string to disable a header for an environment.
        /// </summary>
        public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Headers removed from every response (e.g. X-Powered-By added by IIS or middleware).</summary>
        public List<string> Remove { get; set; } = new();

        public sealed class HstsSettings
        {
            /// <summary>Strict-Transport-Security is only ever sent on HTTPS requests.</summary>
            public bool Enabled { get; set; } = true;
            public int MaxAgeDays { get; set; } = 365;
            public bool IncludeSubDomains { get; set; } = true;
            public bool Preload { get; set; }
            /// <summary>
            /// Extra hosts that never receive HSTS. Loopback addresses (localhost, 127.0.0.1, [::1]) are always excluded,
            /// because a browser would otherwise pin them to HTTPS.
            /// </summary>
            public List<string> ExcludedHosts { get; set; } = new();
        }

        /// <summary>Returns an error message when a configured header name or value cannot be sent, otherwise null.</summary>
        public string? Validate()
        {
            foreach (var (name, value) in Values)
            {
                if (!IsValidHeaderName(name))
                    return $"'{name}' is not a valid HTTP header name.";
                if (value is not null && value.Any(c => c is '\r' or '\n' or '\0'))
                    return $"The value of header '{name}' contains a line break or control character.";
            }

            foreach (var name in Remove)
            {
                if (!IsValidHeaderName(name))
                    return $"'{name}' in Remove is not a valid HTTP header name.";
            }

            if (Hsts.MaxAgeDays < 0)
                return "Hsts:MaxAgeDays cannot be negative.";

            return null;
        }

        private static bool IsValidHeaderName(string? name) =>
            !string.IsNullOrWhiteSpace(name) &&
            name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    }
}
