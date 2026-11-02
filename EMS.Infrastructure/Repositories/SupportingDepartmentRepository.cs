using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class SupportingDepartmentRepository : CatalogRepository<SupportingDepartment>, ISupportingDepartmentRepository
    {
        public SupportingDepartmentRepository(EMSDbContext context) : base(context) { }

        public override async Task<bool> HasActiveDependentsAsync(long id, CancellationToken cancellationToken = default)
        {
            return await _context.Users.AnyAsync(x => x.SupportingDepartmentId == id && x.IsActive, cancellationToken) || await _context.Equipments.AnyAsync(x => x.CurrentSupportingDepartmentId == id && x.IsActive, cancellationToken);
        }
    }
}
