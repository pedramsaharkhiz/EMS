using EMS.Application.Dtos.User;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.Identity;

/// <summary>
/// Fills the post (سمت) title and code of users that already carry a <c>PostId</c>, with one query for all of them. A deactivated post is
/// still shown (a user keeps the post they had), so the soft-delete filter is lifted.
/// </summary>
internal static class UserPostLookup
{
    public static async Task FillAsync(EMSDbContext db, IEnumerable<UserListItemDto> items, CancellationToken cancellationToken)
    {
        var users = items.Where(x => x.PostId.HasValue).ToList();
        if (users.Count == 0)
            return;

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
}
