namespace EMS.Application.Common.Logging
{
    public enum LoginFailureReason
    {
        UnknownUser,
        InactiveUser,
        InvalidPassword,
        LockedOut
    }

    public enum RefreshRejectionReason
    {
        /// <summary>Token hash was not found.</summary>
        TokenNotFound,
        /// <summary>A token that was already revoked was presented again (possible token theft / replay).</summary>
        TokenReuseDetected,
        /// <summary>Token expired.</summary>
        TokenExpired,
        /// <summary>Token row exists but is flagged inactive.</summary>
        TokenInactive,
        /// <summary>The owning user is missing or deactivated.</summary>
        UserInvalid,
        /// <summary>
        /// The same refresh token was used twice at (almost) the same time, e.g. two browser tabs or a client retry.
        /// Only one request can win; the others are rejected, but this is not treated as token theft.
        /// </summary>
        ConcurrentUse
    }

    /// <summary>
    /// Writes authentication / authorization events to the <see cref="LogCategories.Security"/> category.
    /// Implementations must never log passwords, tokens or token hashes.
    /// </summary>
    public interface ISecurityEventLogger
    {
        void LoginSucceeded(long userId, string userName);
        void LoginFailed(string attemptedUserName, LoginFailureReason reason, long? userId = null);
        void RefreshSucceeded(long userId);
        void RefreshRejected(RefreshRejectionReason reason, long? userId = null);
        void RefreshTokenRevoked(long? userId);
        void UserRegistered(long userId, string userName);
        void AccessDenied(string requiredPermission);

        /// <summary>A record that exists was hidden from the user by a row policy (he got 404). Only identifiers and the scope reason are logged.</summary>
        void RowAccessDenied(string entityType, object? entityId, string scopeReason);
        void AuthenticationFailed(string reason);
    }
}
