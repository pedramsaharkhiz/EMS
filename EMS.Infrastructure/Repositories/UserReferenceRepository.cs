using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class UserReferenceRepository : IUserReferenceRepository
    {
        private readonly EMSDbContext _context;

        public UserReferenceRepository(EMSDbContext context) => _context = context;

        public async Task<Dictionary<long, PersonReference>> GetPeopleAsync(IEnumerable<long?> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<long, PersonReference>();

            // The soft-delete filter is lifted for the whole query: a post, a department or a city that was deactivated since still names where the person works.
            return await _context.Users.AsNoTracking().IncludingInactive()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new PersonReference
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    PersonnelCode = u.PersonnelCode,
                    IsActive = u.IsActive,
                    PostId = u.PostId,
                    PostTitle = u.Post!.Title,
                    SupportingDepartmentId = u.SupportingDepartmentId,
                    SupportingDepartmentTitle = u.SupportingDepartment!.Title,
                    CityId = (long?)u.SupportingDepartment!.CityId,
                    CityTitle = u.SupportingDepartment!.City.Title,
                    ProvinceId = (long?)u.SupportingDepartment!.City.ProvinceId,
                    ProvinceTitle = u.SupportingDepartment!.City.Province.Title,
                })
                .ToDictionaryAsync(p => p.Id, cancellationToken);
        }
    }
}
