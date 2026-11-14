using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;

namespace EMS.Infrastructure.Repositories;

public sealed class EmployeePhoneNumberRepository(EMSDbContext context) : Repository<EmployeePhoneNumber>(context), IEmployeePhoneNumberRepository
{
    public IQueryable<EmployeePhoneNumber> QueryAll(bool tracked = false) => Query(asNoTracking: !tracked).IncludingInactive();
    public override void Update(EmployeePhoneNumber entity) => _context.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
}
