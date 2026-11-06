using System.Runtime.CompilerServices;

namespace EMS.Tests.Support
{
    /// <summary>
    /// The application takes its secrets from the environment and refuses to start without them (nothing secret is in the repository). The tests give
    /// it values that exist only here: they are set once, before any test host starts, and protect nothing.
    /// </summary>
    internal static class TestEnvironment
    {
        public const string AdminPassword = "Test-Admin#2026-xK9";

        [ModuleInitializer]
        internal static void Initialize()
        {
            Environment.SetEnvironmentVariable("Jwt__SecretKey", "test-only-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz-ABCDEFGH");
            Environment.SetEnvironmentVariable("Seed__AdminPassword", AdminPassword);
            Environment.SetEnvironmentVariable("ConnectionStrings__EMS", "Server=test-not-used;Database=EMS_unused;Integrated Security=true");
        }
    }
}
