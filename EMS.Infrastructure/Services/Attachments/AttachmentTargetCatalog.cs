using EMS.Application.Interfaces.Attachment;
using EMS.Infrastructure.Persistence;
using EMS.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;

namespace EMS.Infrastructure.Services.Attachments
{
    /// <summary>
    /// Finds which entities can have attachments. An entity is attachable when it carries <see cref="AttachableAttribute"/> and its
    /// row in the Tables registry is active and enabled. Existence of a record is checked from the EF model (table name and key), so a
    /// new attachable entity needs no code of its own.
    /// </summary>
    public sealed class AttachmentTargetCatalog : IAttachmentTargetCatalog
    {
        private readonly EMSDbContext _context;

        public AttachmentTargetCatalog(EMSDbContext context)
        {
            _context = context;
        }

        public async Task<AttachmentTarget?> FindAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var name = tableName.Trim();
            var table = await _context.Tables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableName == name && t.IsActive && t.IsAttachmentEnabled, cancellationToken);
            return table is null ? null : ToTarget(table.TableId, table.TableName, table.DisplayName);
        }

        public async Task<AttachmentTarget?> FindAsync(long tableId, CancellationToken cancellationToken = default)
        {
            var table = await _context.Tables.AsNoTracking()
                .FirstOrDefaultAsync(t => t.TableId == tableId && t.IsActive && t.IsAttachmentEnabled, cancellationToken);
            return table is null ? null : ToTarget(table.TableId, table.TableName, table.DisplayName);
        }

        public Task<bool> EntityExistsAsync(AttachmentTarget target, long entityId, CancellationToken cancellationToken = default)
        {
            var entityType = AttachableEntityTypes(_context).FirstOrDefault(t => t.GetTableName() == target.TableName)
                ?? throw new InvalidOperationException($"No attachable entity maps to table {target.TableName}.");

            var key = entityType.FindPrimaryKey()!.Properties.Single().Name;
            var checkActive = entityType.FindProperty("IsActive") is not null;
            var method = typeof(AttachmentTargetCatalog).GetMethod(nameof(ExistsAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);
            return (Task<bool>)method.Invoke(null, new object[] { _context, key, entityId, checkActive, cancellationToken })!;
        }

        private static Task<bool> ExistsAsync<T>(EMSDbContext context, string key, long id, bool checkActive, CancellationToken cancellationToken) where T : class =>
            checkActive
                ? context.Set<T>().AnyAsync(x => EF.Property<long>(x, key) == id && EF.Property<bool>(x, "IsActive"), cancellationToken)
                : context.Set<T>().AnyAsync(x => EF.Property<long>(x, key) == id, cancellationToken);

        /// <summary>The entity types that carry [Attachable] (shared with the Tables seeder).</summary>
        public static IEnumerable<IEntityType> AttachableEntityTypes(DbContext context) =>
            context.Model.GetEntityTypes().Where(t => t.GetTableName() is not null && t.ClrType.GetCustomAttribute<AttachableAttribute>() is not null);

        private AttachmentTarget? ToTarget(long tableId, string tableName, string displayName)
        {
            var attribute = AttachableEntityTypes(_context)
                .Where(t => t.GetTableName() == tableName)
                .Select(t => t.ClrType.GetCustomAttribute<AttachableAttribute>()!)
                .FirstOrDefault();
            return attribute is null ? null : new AttachmentTarget(tableId, tableName, displayName, attribute.ViewPermission, attribute.EditPermission);
        }
    }
}
