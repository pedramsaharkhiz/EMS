using EMS.Application.Dtos.User;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.Identity;

/// <summary>
/// Fills what a user row carries only as ids: the post (سمت) code and title, and the place of work (supporting department, its city and province),
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

        var withDepartment = all.Where(x => x.SupportingDepartmentId.HasValue).ToList();
        if (withDepartment.Count > 0)
        {
            var ids = withDepartment.Select(x => x.SupportingDepartmentId!.Value).Distinct().ToList();
            var departments = await db.SupportingDepartments.IncludingInactive().AsNoTracking()
                .Where(d => ids.Contains(d.SupportingDepartmentId))
                .Select(d => new
                {
                    d.SupportingDepartmentId, d.Title, CityId = d.CityId, CityTitle = d.City.Title,
                    ProvinceId = d.City.ProvinceId, ProvinceTitle = d.City.Province.Title,
                })
                .ToDictionaryAsync(d => d.SupportingDepartmentId, cancellationToken);

            foreach (var user in withDepartment)
            {
                if (!departments.TryGetValue(user.SupportingDepartmentId!.Value, out var department))
                    continue;
                user.SupportingDepartmentTitle = department.Title;
                user.CityId = department.CityId;
                user.CityTitle = department.CityTitle;
                user.ProvinceId = department.ProvinceId;
                user.ProvinceTitle = department.ProvinceTitle;
            }
        }
    }
}
