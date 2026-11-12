namespace EMS.Application.Interfaces.Auth
{
    public enum AccessTokenValidationResult
    {
        Valid,
        /// <summary>The token carries no security stamp (issued before stamps were introduced).</summary>
        MissingStamp,
        /// <summary>The user no longer exists or has been deactivated.</summary>
        UserNotFoundOrInactive,
        /// <summary>
        /// The user's security stamp changed after the token was issued: roles or role permissions were changed,
        /// or the password was reset. The client must obtain a new token (refresh).
        /// </summary>
        StampMismatch
    }

    /// <summary>
    /// Re-checks a still-cryptographically-valid access token against the user's current state, so that
    /// deactivating a user or changing roles/permissions takes effect immediately instead of after token expiry.
    /// </summary>
    public interface IAccessTokenValidator
    {
        Task<AccessTokenValidationResult> ValidateAsync(long userId, string? tokenSecurityStamp, CancellationToken cancellationToken = default);

        /// <summary>
        /// The same check, plus the user's current supporting department, read in the SAME lookup. Token validation already reads the user on every request;
        /// the data scope (row-level security) rides on it so it costs no extra query.
        /// </summary>
        Task<(AccessTokenValidationResult Result, long? SupportingDepartmentId)> ValidateWithScopeAsync(long userId, string? tokenSecurityStamp, CancellationToken cancellationToken = default);
    }
}
