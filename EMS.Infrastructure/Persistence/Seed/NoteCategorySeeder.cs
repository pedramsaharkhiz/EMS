using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EMS.Infrastructure.Persistence.Seed;

public interface INoteCategorySeeder
{
    Task<int> SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>Like the geography seed: add missing titles, counting inactive rows too, without overwriting user data.</summary>
public sealed class NoteCategorySeeder : INoteCategorySeeder
{
    private readonly EMSDbContext _context;
    private readonly ILogger<NoteCategorySeeder> _logger;

    public NoteCategorySeeder(EMSDbContext context, ILogger<NoteCategorySeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _context.Set<NoteCategory>().AsNoTracking().IncludingInactive()
            .Select(x => x.Title).ToListAsync(cancellationToken);
        var titles = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = InitialTitles.Where(title => !titles.Contains(title))
            .Select(title => new NoteCategory { Title = title, CreatedByUserId = 0 }).ToList();
        if (missing.Count > 0)
        {
            _context.Set<NoteCategory>().AddRange(missing);
            await _context.SaveChangesAsync(cancellationToken);
        }
        _logger.LogInformation("Note category seed: added {Categories} categories", missing.Count);
        return missing.Count;
    }

    private static readonly string[] InitialTitles =
    {
        "پیگیری", "هشدار", "تذکر", "هماهنگی", "درخواست اطلاعات", "راهنمایی فنی"
    };
}
