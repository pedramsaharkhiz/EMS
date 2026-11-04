using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Services.Attachments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace EMS.Infrastructure.Persistence.Seed;

public interface IAttachmentTableSeeder
{
    /// <summary>Brings the Tables registry in line with the [Attachable] entities; returns how many rows were added and changed.</summary>
    Task<(int Added, int Changed)> SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fills the Tables registry from the entities that carry <see cref="AttachableAttribute"/>, at every start: a new attachable entity gets
/// its row (enabled, with its Persian name), a renamed one is updated, and an entity that lost the attribute is only switched off
/// (IsAttachmentEnabled = false), never deleted, because its old attachments still point at the row. Idempotent.
/// </summary>
public sealed class AttachmentTableSeeder : IAttachmentTableSeeder
{
    private readonly EMSDbContext _context;
    private readonly ILogger<AttachmentTableSeeder> _logger;

    public AttachmentTableSeeder(EMSDbContext context, ILogger<AttachmentTableSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int Added, int Changed)> SeedAsync(CancellationToken cancellationToken = default)
    {
        var wanted = AttachmentTargetCatalog.AttachableEntityTypes(_context)
            .ToDictionary(t => t.GetTableName()!, t => t.ClrType.GetCustomAttribute<AttachableAttribute>()!.DisplayName);

        var rows = await _context.Tables.ToListAsync(cancellationToken);
        int added = 0, changed = 0;

        foreach (var (tableName, displayName) in wanted)
        {
            var row = rows.FirstOrDefault(r => r.TableName == tableName);
            if (row is null)
            {
                _context.Tables.Add(new Table { TableName = tableName, DisplayName = displayName, IsAttachmentEnabled = true });
                added++;
            }
            else if (row.DisplayName != displayName || !row.IsAttachmentEnabled || !row.IsActive)
            {
                row.DisplayName = displayName;
                row.IsAttachmentEnabled = true;
                row.IsActive = true;
                changed++;
            }
        }

        foreach (var row in rows.Where(r => r.IsAttachmentEnabled && !wanted.ContainsKey(r.TableName)))
        {
            row.IsAttachmentEnabled = false;
            changed++;
        }

        if (added + changed > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Attachment tables synced: {Added} added, {Changed} changed", added, changed);
        }

        return (added, changed);
    }
}
