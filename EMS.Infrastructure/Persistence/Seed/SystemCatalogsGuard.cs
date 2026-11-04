using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Persistence.Seed;

public interface ISystemCatalogsGuard
{
    /// <summary>Makes sure the system rows of the report types and the failure report statuses exist and are intact.</summary>
    Task<(int Created, int Repaired)> EnsureAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Same guarantee as <see cref="SystemPostsGuard"/> for the other catalogs whose rows the code relies on: the report types
/// (<see cref="ReportTypeCode"/>) and the failure report statuses (<see cref="FailureReportStatusCode"/>). A missing row is
/// created with its fixed id, a deactivated one is reactivated and a changed code is restored. Edited titles are never touched.
/// Idempotent; runs at every start.
/// </summary>
public sealed class SystemCatalogsGuard : ISystemCatalogsGuard
{
    private readonly EMSDbContext _context;
    private readonly ILogger<SystemCatalogsGuard> _logger;

    public SystemCatalogsGuard(EMSDbContext context, ILogger<SystemCatalogsGuard> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int Created, int Repaired)> EnsureAsync(CancellationToken cancellationToken = default)
    {
        var reportTypes = await EnsureAsync<ReportType, ReportTypeCode>(
            _context.ReportTypes, "ReportTypes", ReportTypeCatalog.Titles, ReportTypeCatalog.IdOf,
            r => r.ReportTypeId, (r, id) => r.ReportTypeId = id, r => r.Code, (r, c) => r.Code = c, cancellationToken);
        var statuses = await EnsureAsync<FailureReportStatus, FailureReportStatusCode>(
            _context.FailureReportStatuses, "FailureReportStatuses", FailureReportStatusCatalog.Titles, FailureReportStatusCatalog.IdOf,
            r => r.FailureReportStatusId, (r, id) => r.FailureReportStatusId = id, r => r.Code, (r, c) => r.Code = c, cancellationToken);

        var result = (reportTypes.Created + statuses.Created, reportTypes.Repaired + statuses.Repaired);
        if (result != (0, 0))
            _logger.LogWarning("System catalog rows repaired: {Created} created, {Repaired} repaired", result.Item1, result.Item2);
        return result;
    }

    private async Task<(int Created, int Repaired)> EnsureAsync<TEntity, TCode>(
        DbSet<TEntity> set, string table, IReadOnlyDictionary<TCode, string> titles, Func<TCode, long> idOf,
        Func<TEntity, long> getId, Action<TEntity, long> setId, Func<TEntity, string?> getCode, Action<TEntity, string?> setCode,
        CancellationToken cancellationToken)
        where TEntity : BaseEntity, ICatalogEntity, new()
        where TCode : struct, Enum
    {
        var rows = await set.IncludingInactive().ToListAsync(cancellationToken);
        var missing = new List<TEntity>();
        var repaired = 0;

        foreach (var (code, title) in titles)
        {
            var id = idOf(code);
            var row = rows.FirstOrDefault(r => getId(r) == id);
            if (row is null)
            {
                // Another active row may already carry the title (the unique index is on active titles): take a free one.
                var free = rows.Any(r => r.IsActive && string.Equals(r.Title, title, StringComparison.OrdinalIgnoreCase)) ? $"{title} (سیستمی)" : title;
                var created = new TEntity { Title = free, IsActive = true, CreatedByUserId = 0, CreatedDateTime = SystemPostsGuard.SeedDate };
                setId(created, id);
                setCode(created, code.ToString());
                missing.Add(created);
                continue;
            }

            var changed = false;
            if (!row.IsActive) { row.IsActive = true; changed = true; }
            if (!string.Equals(getCode(row), code.ToString(), StringComparison.Ordinal)) { setCode(row, code.ToString()); changed = true; }
            if (changed) repaired++;
        }

        if (missing.Count == 0 && repaired == 0)
            return (0, 0);

        if (missing.Count > 0 && _context.Database.IsSqlServer())
        {
            // the ids are fixed, but the key is an identity column: inserting them needs IDENTITY_INSERT on the same connection
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{table}] ON", cancellationToken);
            set.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{table}] OFF", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            set.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return (missing.Count, repaired);
    }
}
