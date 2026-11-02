using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Logging
{
    public sealed class AuditEventLogger : IAuditEventLogger
    {
        private readonly ILogger _logger;
        private readonly ICurrentUserService _currentUser;

        public AuditEventLogger(ILoggerFactory loggerFactory, ICurrentUserService currentUser)
        {
            _logger = loggerFactory.CreateLogger(LogCategories.Audit);
            _currentUser = currentUser;
        }

        public void Created(string entityType, object? entityId) =>
            _logger.LogInformation(
                "{EntityType} {EntityId} created by user {ActorId} ({ActorName})",
                entityType, entityId, _currentUser.UserId, LogSanitizer.Sanitize(_currentUser.UserName));

        public void Updated(string entityType, object? entityId) =>
            _logger.LogInformation(
                "{EntityType} {EntityId} updated by user {ActorId} ({ActorName})",
                entityType, entityId, _currentUser.UserId, LogSanitizer.Sanitize(_currentUser.UserName));

        public void Deleted(string entityType, object? entityId) =>
            _logger.LogInformation(
                "{EntityType} {EntityId} deleted (deactivated) by user {ActorId} ({ActorName})",
                entityType, entityId, _currentUser.UserId, LogSanitizer.Sanitize(_currentUser.UserName));

        public void Operation(string operation, string entityType, object? entityId, string? detail = null) =>
            _logger.LogInformation(
                "{Operation} on {EntityType} {EntityId} by user {ActorId} ({ActorName}) {Detail}",
                operation, entityType, entityId, _currentUser.UserId,
                LogSanitizer.Sanitize(_currentUser.UserName), detail);
    }
}
