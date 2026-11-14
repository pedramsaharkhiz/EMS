using EMS.Application.Dtos.Common;
using EMS.Application.Dtos.User;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.Identity;

/// <summary>
/// Fills what a user row carries only as ids: the post (سمت) code and title, and the places of work (every supporting department with its city and province),
/// with one query each for all users of the page. A deactivated post or department is still shown (a user keeps what he had), so the soft-delete
/// filter is lifted.
/// </summary>
internal static class UserPostLookup
{
    public static async Task FillAsync(EMSDbContext db, IEnumerable<UserListItemDto> items, CancellationToken cancellationToken)
    {
        var all = items.ToList();

        var users = all.Where(x => x.PostId.HasValue).ToList();
        if (users.Count > 0)
        {
            var ids = users.Select(x => x.PostId!.Value).Distinct().ToList();
            var posts = await db.Posts.IncludingInactive().AsNoTracking()
                .Where(p => ids.Contains(p.PostId))
                .Select(p => new { p.PostId, p.Code, p.Title })
                .ToDictionaryAsync(p => p.PostId, cancellationToken);

            foreach (var user in users)
            {
                if (!posts.TryGetValue(user.PostId!.Value, out var post))
                    continue;
                user.PostCode = post.Code;
                user.PostTitle = post.Title;
            }
        }

        // the departments of every user of the page: one query (the soft-delete filter is lifted, a deactivated department still names where he works)
        var userIds = all.Select(x => x.Id).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var memberships = await (
                from m in db.UserSupportingDepartments.AsNoTracking()
                join d in db.SupportingDepartments.IncludingInactive().AsNoTracking() on m.SupportingDepartmentId equals d.SupportingDepartmentId
                where userIds.Contains(m.UserId)
                select new
                {
                    m.UserId, DepartmentId = d.SupportingDepartmentId, d.Title, CityId = d.CityId, CityTitle = d.City.Title,
                    ProvinceId = d.City.ProvinceId, ProvinceTitle = d.City.Province.Title,
                }).ToListAsync(cancellationToken);

            var byUser = memberships.GroupBy(m => m.UserId).ToDictionary(g => g.Key);
            foreach (var user in all)
            {
                user.SupportingDepartments = byUser.TryGetValue(user.Id, out var rows)
                    ? rows.OrderBy(r => r.Title, StringComparer.Ordinal).ThenBy(r => r.DepartmentId)
                        .Select(r => new PersonDepartmentDto { Id = r.DepartmentId, Title = r.Title, CityId = r.CityId, CityTitle = r.CityTitle, ProvinceId = r.ProvinceId, ProvinceTitle = r.ProvinceTitle })
                        .ToList()
                    : [];
            }
        }
    }
}
