using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories;

public sealed class SkillRepository
    : CatalogRepository<Skill>, ISkillRepository
{
    public SkillRepository(EMSDbContext context)
        : base(context)
    {
    }

    public IQueryable<Skill> QueryForRead()
    {
        return _context.Set<Skill>()
            .AsNoTracking()
            .IncludingInactive();
    }

    public override Task<bool> HasActiveDependentsAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _context.TechnicianSkills
            .AnyAsync(
                x => x.SkillId == id && x.IsActive,
                cancellationToken);
    }
}
