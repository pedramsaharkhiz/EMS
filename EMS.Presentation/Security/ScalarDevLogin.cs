using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;

namespace EMS.Presentation.Security
{
    /// <summary>
    /// Development-only convenience for the Scalar documentation page: every time the page is opened the server signs in
    /// with the configured development account and hands the fresh access token to Scalar, so nobody has to copy a token
    /// out of the browser dev tools. Configured under <c>Scalar:DevLogin</c> (UserName / Password); with no credentials
    /// configured nothing happens and Scalar keeps asking for the token as usual.
    /// </summary>
    internal static class ScalarDevLogin
    {
        public const string SectionName = "Scalar:DevLogin";

        private static bool IsLocalRequest(HttpContext httpContext)
        {
            var remote = httpContext.Connection.RemoteIpAddress;
            return remote is null
                || System.Net.IPAddress.IsLoopback(remote)
                || remote.Equals(httpContext.Connection.LocalIpAddress);
        }

        /// <summary>
        /// Returns a fresh access token for the page request, or <c>null</c> when the feature is not configured, the request
        /// does not come from this machine, or the sign-in fails (a warning is logged and Scalar simply shows no token).
        /// </summary>
        public static async Task<string?> TryGetTokenAsync(HttpContext httpContext)
        {
            var section = httpContext.RequestServices.GetRequiredService<IConfiguration>().GetSection(SectionName);
            var userName = section["UserName"];
            var password = section["Password"];
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
                return null;

            // The token ends up inside the page HTML: never hand an administrator token to another machine on the network.
            if (!IsLocalRequest(httpContext))
                return null;

            // Scalar re-requests the page's assets with the same endpoint; only the HTML document needs a token.
            if (!httpContext.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase))
                return null;

            try
            {
                var auth = httpContext.RequestServices.GetRequiredService<IAuthService>();
                var result = await auth.LoginAsync(
                    new LoginRequestDto { UserName = userName, Password = password },
                    httpContext.RequestAborted);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(ScalarDevLogin))
                    .LogWarning(ex, "Scalar dev login failed for user {UserName}; Scalar will ask for the token manually", userName);
                return null;
            }
        }
    }
}
