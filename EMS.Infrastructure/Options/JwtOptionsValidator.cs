using System.Text;

namespace EMS.Infrastructure.Options
{
    /// <summary>
    /// The signing key is a secret: it comes from the environment (<c>Jwt__SecretKey</c>) or the secret store, never from a file in the repository.
    /// The application refuses to start without a strong one, so a forgotten setting can not silently leave tokens forgeable.
    /// </summary>
    public static class JwtOptionsValidator
    {
        /// <summary>HS256 needs a key of at least 256 bits.</summary>
        public const int MinimumKeyBytes = 32;

        /// <summary>The value that used to be committed in appsettings.json. It is public, so it is never accepted.</summary>
        private const string LeakedKey = "THIS_IS_A_VERY_STRONG_SECRET_KEY_FOR_EMS_2026";

        /// <summary>The reason the options are not acceptable, or null when they are.</summary>
        public static string? Validate(JwtOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SecretKey))
                return "Jwt:SecretKey is not set. Provide it through the environment variable Jwt__SecretKey or the user secrets (see docs/SECRETS.md).";
            if (Encoding.UTF8.GetByteCount(options.SecretKey) < MinimumKeyBytes)
                return $"Jwt:SecretKey is too short: at least {MinimumKeyBytes} bytes (a random 64-character value is recommended).";
            if (string.Equals(options.SecretKey, LeakedKey, StringComparison.Ordinal))
                return "Jwt:SecretKey is the value that was published in the repository. Generate a new random key.";
            if (string.IsNullOrWhiteSpace(options.Issuer) || string.IsNullOrWhiteSpace(options.Audience))
                return "Jwt:Issuer and Jwt:Audience must be set.";
            if (options.AccessTokenExpirationMinutes <= 0 || options.RefreshTokenExpirationDays <= 0)
                return "Jwt token lifetimes must be positive.";
            return null;
        }
    }
}
