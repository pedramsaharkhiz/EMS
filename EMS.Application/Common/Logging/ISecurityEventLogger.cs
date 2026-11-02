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
        UserInvalid
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
        void AuthenticationFailed(string reason);
    }
}
