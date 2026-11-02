using Microsoft.AspNetCore.Http;

namespace EMS.Infrastructure.Options
{
    /// <summary>
    /// Security attributes for the cookies of the ASP.NET Core Identity cookie schemes. Bound from <c>Security:Cookies</c>.
    /// The API authenticates with a bearer token and issues no cookie today; these settings make sure that any cookie
    /// that is ever issued (e.g. if a SignIn call is added) is created with safe attributes.
    /// </summary>
    public sealed class CookieSecurityOptions
    {
        public const string SectionName = "Security:Cookies";

        /// <summary>Not readable from JavaScript (protects against theft through XSS).</summary>
        public bool HttpOnly { get; set; } = true;

        /// <summary>Always = only sent over HTTPS. SameAsRequest is acceptable only for local development over http.</summary>
        public CookieSecurePolicy SecurePolicy { get; set; } = CookieSecurePolicy.Always;

        /// <summary>Strict = never sent on cross-site requests (CSRF protection).</summary>
        public SameSiteMode SameSite { get; set; } = SameSiteMode.Strict;

        /// <summary>Lifetime of the application (sign-in) cookie.</summary>
        public int ExpireMinutes { get; set; } = 30;

        /// <summary>Extend the application cookie on activity. Off by default: a session cannot live forever.</summary>
        public bool SlidingExpiration { get; set; }
    }
}
