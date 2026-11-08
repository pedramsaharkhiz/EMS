using EMS.Domain;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Persistence.Seed;

public interface ISystemPostsGuard
{
    /// <summary>Makes sure the two system posts exist and are intact; returns how many rows were created and repaired.</summary>
    Task<(int Created, int Repaired)> EnsureAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The code relies on two posts (صف، ستاد; <see cref="Domain.Enums.PostCode"/>). The migration inserts them and the
/// API refuses to delete, deactivate or re-code them, but nothing stops someone from changing the table directly. This runs at every
/// start and puts them back: a missing row is created with its fixed id, a deactivated one is reactivated and a changed code is restored.
/// Titles that administrators edited are never touched. Idempotent.
/// </summary>
public sealed class SystemPostsGuard : ISystemPostsGuard
{
    /// <summary>The fixed date of the seeded rows (seed data must be deterministic).</summary>
    public static readonly DateTime SeedDate = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

    private readonly EMSDbContext _context;
    private readonly ILogger<SystemPostsGuard> _logger;

    public SystemPostsGuard(EMSDbContext context, ILogger<SystemPostsGuard> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(int Created, int Repaired)> EnsureAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _context.Posts.IncludingInactive().ToListAsync(cancellationToken);
        var missing = new List<Post>();
        var repaired = 0;

        foreach (var (code, title) in PostCatalog.Titles)
        {
            var id = PostCatalog.IdOf(code);
            var row = rows.FirstOrDefault(r => r.PostId == id);
            if (row is null)
            {
                // Another active post may already carry the title (the unique index is on active titles): take a free one.
                var free = rows.Any(r => r.IsActive && string.Equals(r.Title, title, StringComparison.OrdinalIgnoreCase)) ? $"{title} (سیستمی)" : title;
                missing.Add(new Post { PostId = id, Title = free, Code = code.ToString(), IsActive = true, CreatedByUserId = 0, CreatedDateTime = SeedDate });
                continue;
            }

            var changed = false;
            if (!row.IsActive) { row.IsActive = true; changed = true; }
            if (!string.Equals(row.Code, code.ToString(), StringComparison.Ordinal)) { row.Code = code.ToString(); changed = true; }
            if (changed) repaired++;
        }

        if (missing.Count == 0 && repaired == 0)
            return (0, 0);

        if (missing.Count > 0 && _context.Database.IsSqlServer())
        {
            // the ids are fixed, but the key is an identity column: inserting them needs IDENTITY_INSERT on the same connection
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Posts] ON", cancellationToken);
            _context.Posts.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT [Posts] OFF", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        else
        {
            _context.Posts.AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogWarning("System posts repaired: {Created} created, {Repaired} repaired", missing.Count, repaired);
        return (missing.Count, repaired);
    }
}
