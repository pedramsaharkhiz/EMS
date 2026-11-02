using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EMS.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// 1. Always: stamps Created/Modified user and time on <see cref="IAuditableEntity"/> rows.
    /// 2. When <c>Logging:AuditTrail:Enabled</c> is true: records every insert/update/delete with old and new
    ///    values in the <c>AuditLogs</c> table (written right after the main save, so generated keys are known).
    ///    Sensitive columns are masked. A failure to write the trail is logged but never fails the business operation.
    /// </summary>
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private const string Masked = "***";

        /// <summary>Entities that are never audited (the trail itself, and high-churn token rows).</summary>
        private static readonly HashSet<Type> ExcludedEntityTypes = new() { typeof(AuditLog), typeof(UserRefreshToken) };

        /// <summary>Bookkeeping columns whose changes carry no information.</summary>
        private static readonly HashSet<string> NoisyProperties = new(StringComparer.OrdinalIgnoreCase)
        {
            nameof(IAuditableEntity.ModifiedDateTime),
            nameof(IAuditableEntity.ModifiedByUserId),
            "ConcurrencyStamp"
        };

        private readonly ICurrentUserService _currentUserService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditSaveChangesInterceptor> _logger;
        private readonly bool _auditTrailEnabled;

        private List<PendingAudit>? _pending;
        private bool _writingAuditTrail;

        public AuditSaveChangesInterceptor(
            ICurrentUserService currentUserService,
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuditTrailOptions> auditTrailOptions,
            ILogger<AuditSaveChangesInterceptor> logger)
        {
            _currentUserService = currentUserService;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _auditTrailEnabled = auditTrailOptions.Value.Enabled;
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            ApplyAudit(eventData.Context);
            CapturePendingAudit(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyAudit(eventData.Context);
            CapturePendingAudit(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            WriteAuditTrail(eventData.Context);
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            await WriteAuditTrailAsync(eventData.Context, cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            if (!_writingAuditTrail)
                _pending = null;
            base.SaveChangesFailed(eventData);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (!_writingAuditTrail)
                _pending = null;
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        // ---------------------------------------------------------------------------------
        // Created / Modified stamps
        // ---------------------------------------------------------------------------------

        private void ApplyAudit(DbContext? context)
        {
            if (context is null)
                return;

            var userId = _currentUserService.UserId;
            var now = DateTime.UtcNow;

            foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedDateTime = now;
                    entry.Entity.CreatedByUserId ??= userId ?? 0;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedDateTime)).IsModified = false;
                    entry.Property(nameof(IAuditableEntity.CreatedByUserId)).IsModified = false;

                    entry.Entity.ModifiedDateTime = now;
                    entry.Entity.ModifiedByUserId = userId;
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Audit trail (old / new values)
        // ---------------------------------------------------------------------------------

        private void CapturePendingAudit(DbContext? context)
        {
            // The save that persists the trail itself must not be captured again.
            if (_writingAuditTrail)
                return;

            _pending = null;

            if (!_auditTrailEnabled || context is null)
                return;

            List<PendingAudit>? pending = null;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                    continue;
                if (ExcludedEntityTypes.Contains(entry.Metadata.ClrType))
                    continue;

                var item = BuildPending(entry);
                if (item is not null)
                    (pending ??= new List<PendingAudit>()).Add(item);
            }

            _pending = pending;
        }

        private static PendingAudit? BuildPending(EntityEntry entry)
        {
            var tableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name;
            var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties ?? new List<Microsoft.EntityFrameworkCore.Metadata.IProperty>();

            switch (entry.State)
            {
                case EntityState.Added:
                {
                    var newValues = new Dictionary<string, object?>();
                    foreach (var property in entry.Properties)
                    {
                        // Store-generated keys have no real value yet; they are filled in after the save.
                        if (property.IsTemporary)
                            continue;
                        newValues[property.Metadata.Name] = MaskIfSensitive(property.Metadata.Name, property.CurrentValue);
                    }

                    return new PendingAudit(tableName, "Insert", entry, null, null, newValues, null);
                }

                case EntityState.Deleted:
                {
                    var oldValues = new Dictionary<string, object?>();
                    foreach (var property in entry.Properties)
                        oldValues[property.Metadata.Name] = MaskIfSensitive(property.Metadata.Name, property.OriginalValue);

                    return new PendingAudit(tableName, "Delete", entry, KeyOf(entry, keyProperties, original: true), oldValues, null, null);
                }

                default: // Modified
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();
                    var changed = new List<string>();

                    foreach (var property in entry.Properties)
                    {
                        var name = property.Metadata.Name;
                        // Update() on a tracked entity flags every column as modified, so compare the values.
                        if (!property.IsModified || NoisyProperties.Contains(name) || Equals(property.OriginalValue, property.CurrentValue))
                            continue;

                        changed.Add(name);
                        oldValues[name] = MaskIfSensitive(name, property.OriginalValue);
                        newValues[name] = MaskIfSensitive(name, property.CurrentValue);
                    }

                    if (changed.Count == 0)
                        return null;

                    return new PendingAudit(tableName, "Update", entry, KeyOf(entry, keyProperties, original: true), oldValues, newValues, changed);
                }
            }
        }

        private static string KeyOf(
            EntityEntry entry,
            IReadOnlyList<Microsoft.EntityFrameworkCore.Metadata.IProperty> keyProperties,
            bool original)
        {
            return string.Join("|", keyProperties.Select(key =>
            {
                var property = entry.Property(key.Name);
                return Convert.ToString(original ? property.OriginalValue : property.CurrentValue, System.Globalization.CultureInfo.InvariantCulture);
            }));
        }

        private void WriteAuditTrail(DbContext? context)
        {
            var logs = BuildAuditLogs(context);
            if (logs is null)
                return;

            _writingAuditTrail = true;
            try
            {
                context!.Set<AuditLog>().AddRange(logs);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                OnAuditTrailWriteFailed(context!, logs, ex);
            }
            finally
            {
                _writingAuditTrail = false;
                _pending = null;
            }
        }

        private async Task WriteAuditTrailAsync(DbContext? context, CancellationToken cancellationToken)
        {
            var logs = BuildAuditLogs(context);
            if (logs is null)
                return;

            _writingAuditTrail = true;
            try
            {
                context!.Set<AuditLog>().AddRange(logs);
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                OnAuditTrailWriteFailed(context!, logs, ex);
            }
            finally
            {
                _writingAuditTrail = false;
                _pending = null;
            }
        }

        private List<AuditLog>? BuildAuditLogs(DbContext? context)
        {
            if (_writingAuditTrail || context is null || _pending is null || _pending.Count == 0)
                return null;

            var httpContext = _httpContextAccessor.HttpContext;
            var userId = _currentUserService.UserId;
            var userName = Truncate(_currentUserService.UserName, 256);
            var ip = Truncate(httpContext?.Connection.RemoteIpAddress?.ToString(), 45);
            var correlationId = Truncate(httpContext?.TraceIdentifier, 64);
            var now = DateTime.UtcNow;

            var logs = new List<AuditLog>(_pending.Count);
            foreach (var item in _pending)
            {
                var newValues = item.NewValues;
                var entityId = item.EntityId;

                if (item.Action == "Insert")
                {
                    // Generated keys are known only now that the row has been written.
                    var keyProperties = item.Entry.Metadata.FindPrimaryKey()?.Properties
                                        ?? new List<Microsoft.EntityFrameworkCore.Metadata.IProperty>();
                    foreach (var key in keyProperties)
                        newValues![key.Name] = item.Entry.Property(key.Name).CurrentValue;
                    entityId = KeyOf(item.Entry, keyProperties, original: false);
                }

                logs.Add(new AuditLog
                {
                    TableName = Truncate(item.TableName, 128)!,
                    EntityId = Truncate(entityId, 256) ?? string.Empty,
                    Action = item.Action,
                    OldValues = item.OldValues is null ? null : JsonSerializer.Serialize(item.OldValues),
                    NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues),
                    ChangedColumns = item.ChangedColumns is null ? null : string.Join(",", item.ChangedColumns),
                    UserId = userId,
                    UserName = userName,
                    IpAddress = ip,
                    CorrelationId = correlationId,
                    TimestampUtc = now
                });
            }

            return logs;
        }

        private void OnAuditTrailWriteFailed(DbContext context, List<AuditLog> logs, Exception ex)
        {
            // Do not fail the already-committed business operation; make the loss visible instead.
            _logger.LogError(ex,
                "Failed to write {Count} audit trail row(s). Is the AuditLogs table present (AddAuditLog migration applied)?",
                logs.Count);

            foreach (var entry in context.ChangeTracker.Entries<AuditLog>().ToList())
                entry.State = EntityState.Detached;
        }

        private static object? MaskIfSensitive(string propertyName, object? value)
        {
            if (value is null)
                return null;

            if (propertyName.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
                propertyName.Equals("SecurityStamp", StringComparison.OrdinalIgnoreCase))
            {
                return Masked;
            }

            return value;
        }

        private static string? Truncate(string? value, int maxLength) =>
            value is null || value.Length <= maxLength ? value : value[..maxLength];

        private sealed record PendingAudit(
            string TableName,
            string Action,
            EntityEntry Entry,
            string? EntityId,
            Dictionary<string, object?>? OldValues,
            Dictionary<string, object?>? NewValues,
            List<string>? ChangedColumns);
    }
}
