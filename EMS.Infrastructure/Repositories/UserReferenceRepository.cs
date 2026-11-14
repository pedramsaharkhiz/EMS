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

        public IQueryable<UserDirectoryRow> QueryDirectory() =>
            _context.Users.AsNoTracking().Select(u => new UserDirectoryRow { Id = u.Id, SupportingDepartmentIds = u.SupportingDepartments.Select(d => d.SupportingDepartmentId) });

        public async Task<Dictionary<long, PersonReference>> GetPeopleAsync(IEnumerable<long?> userIds, CancellationToken cancellationToken = default)
        {
            var ids = userIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<long, PersonReference>();

            // The soft-delete filter is lifted for the whole query: a post, a department or a city that was deactivated since still names where the person works.
            var people = await _context.Users.AsNoTracking().IncludingInactive()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new PersonReference
                {
                    Id = u.Id,
                    FullName = u.FirstName + " " + u.LastName,
                    PersonnelCode = u.PersonnelCode,
                    IsActive = u.IsActive,
                    PostId = u.PostId,
                    PostTitle = u.Post!.Title,
                })
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            // every department of these people, in one more query
            var memberships = await (
                from m in _context.UserSupportingDepartments.AsNoTracking()
                join d in _context.SupportingDepartments.IncludingInactive().AsNoTracking() on m.SupportingDepartmentId equals d.SupportingDepartmentId
                where ids.Contains(m.UserId)
                select new
                {
                    m.UserId,
                    Department = new PersonDepartment
                    {
                        Id = d.SupportingDepartmentId, Title = d.Title, CityId = d.CityId, CityTitle = d.City.Title,
                        ProvinceId = d.City.ProvinceId, ProvinceTitle = d.City.Province.Title,
                    },
                }).ToListAsync(cancellationToken);

            foreach (var group in memberships.GroupBy(m => m.UserId))
            {
                if (people.TryGetValue(group.Key, out var person))
                    person.SupportingDepartments = group.Select(m => m.Department).OrderBy(d => d.Title, StringComparer.Ordinal).ThenBy(d => d.Id).ToList();
            }

            return people;
        }
    }
}
