using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace EMS.Presentation.Security
{
    public static class SecurityHeadersExtensions
    {
        /// <summary>
        /// Controls the "Server: Kestrel" response header through <c>Security:Headers:RemoveServerHeader</c>
        /// (default: removed). Has no effect behind IIS or a reverse proxy, which add their own Server header.
        /// </summary>
        public static IWebHostBuilder ConfigureServerHeaderFromConfiguration(this IWebHostBuilder webHost)
        {
            return webHost.ConfigureKestrel((context, kestrel) =>
                kestrel.AddServerHeader = !context.Configuration.GetValue($"{SecurityHeadersOptions.SectionName}:RemoveServerHeader", true));
        }
    }
}
