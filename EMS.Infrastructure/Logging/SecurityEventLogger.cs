using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Logging
{
    public sealed class SecurityEventLogger : ISecurityEventLogger
    {
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICurrentUserService _currentUser;

        public SecurityEventLogger(
            ILoggerFactory loggerFactory,
            IHttpContextAccessor httpContextAccessor,
            ICurrentUserService currentUser)
        {
            _logger = loggerFactory.CreateLogger(LogCategories.Security);
            _httpContextAccessor = httpContextAccessor;
            _currentUser = currentUser;
        }

        private string? ClientIp => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        private string? UserAgent =>
            LogSanitizer.Sanitize(_httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString(), 200);

        public void LoginSucceeded(long userId, string userName) =>
            _logger.LogInformation(
                "Login succeeded for user {UserId} ({UserName}) from {ClientIp} [{UserAgent}]",
                userId, LogSanitizer.Sanitize(userName), ClientIp, UserAgent);

        public void LoginFailed(string attemptedUserName, LoginFailureReason reason, long? userId = null) =>
            _logger.LogWarning(
                "Login failed ({Reason}) for user name {AttemptedUserName} (UserId {UserId}) from {ClientIp} [{UserAgent}]",
                reason, LogSanitizer.Sanitize(attemptedUserName), userId, ClientIp, UserAgent);

        public void RefreshSucceeded(long userId) =>
            _logger.LogInformation(
                "Refresh token rotated for user {UserId} from {ClientIp}", userId, ClientIp);

        public void RefreshRejected(RefreshRejectionReason reason, long? userId = null)
        {
            var level = reason == RefreshRejectionReason.TokenReuseDetected ? LogLevel.Error : LogLevel.Warning;
            _logger.Log(level,
                "Refresh token rejected ({Reason}) for user {UserId} from {ClientIp} [{UserAgent}]",
                reason, userId, ClientIp, UserAgent);
        }

        public void RefreshTokenRevoked(long? userId) =>
            _logger.LogInformation(
                "Refresh token revoked by request (UserId {UserId}) from {ClientIp}", userId, ClientIp);

        public void UserRegistered(long userId, string userName) =>
            _logger.LogInformation(
                "User {UserId} ({UserName}) registered from {ClientIp}",
                userId, LogSanitizer.Sanitize(userName), ClientIp);

        public void AccessDenied(string requiredPermission) =>
            _logger.LogWarning(
                "Access denied: user {UserId} ({UserName}) lacks permission {RequiredPermission} for {Method} {Path} from {ClientIp}",
                _currentUser.UserId, LogSanitizer.Sanitize(_currentUser.UserName), requiredPermission,
                _httpContextAccessor.HttpContext?.Request.Method,
                _httpContextAccessor.HttpContext?.Request.Path.Value,
                ClientIp);

        public void AuthenticationFailed(string reason) =>
            _logger.LogWarning(
                "Authentication failed ({Reason}) for {Method} {Path} from {ClientIp} [{UserAgent}]",
                LogSanitizer.Sanitize(reason, 200),
                _httpContextAccessor.HttpContext?.Request.Method,
                _httpContextAccessor.HttpContext?.Request.Path.Value,
                ClientIp, UserAgent);
    }
}
