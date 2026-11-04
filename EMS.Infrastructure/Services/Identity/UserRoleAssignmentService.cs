using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;
using EMS.Application.Dtos.UserRole;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.Identity;

public sealed class UserRoleAssignmentService
    : IUserRoleAssignmentService
{
    public const string SingleRoleMessage = "هر کاربر فقط یک نقش می‌تواند داشته باشد.";
    public const string AlreadyHasRoleMessage = "این کاربر نقش دیگری دارد. هر کاربر فقط یک نقش می‌تواند داشته باشد؛ نقش فعلی را بردارید یا با تنظیم نقش جایگزین کنید.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly EMSDbContext _dbContext;
    private readonly IAuditEventLogger _audit;

    public UserRoleAssignmentService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        EMSDbContext dbContext,
        IAuditEventLogger audit)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _audit = audit;
    }

    // ---------------------------------------------------------
    // User -> Roles
    // ---------------------------------------------------------

    public async Task<IReadOnlyList<UserRoleItemDto>> GetUserRolesAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        await FindUserAsync(userId, cancellationToken);

        return await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _roleManager.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where userRole.UserId == userId
                  && userRole.IsActive
            orderby role.Name
            select new UserRoleItemDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                IsActive = role.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task SetUserRolesAsync(
        long userId,
        IReadOnlyCollection<long> roleIds,
        CancellationToken cancellationToken = default)
    {
        await FindUserAsync(userId, cancellationToken);

        var requestedRoleIds = roleIds.Distinct().ToHashSet();

        if (requestedRoleIds.Count > 1)
        {
            throw new FluentValidation.ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("roleIds", SingleRoleMessage)
            });
        }

        var validRoleIds = await _roleManager.Roles
            .Where(role =>
                requestedRoleIds.Contains(role.Id) &&
                role.IsActive)
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        if (validRoleIds.Count != requestedRoleIds.Count)
        {
            throw new BadRequestException(
                "یک یا چند نقش وجود ندارد یا غیرفعال است.", ErrorCodes.RolesInvalid);
        }

        var existingRelations = await _dbContext.UserRoles
            .Where(relation => relation.UserId == userId)
            .ToListAsync(cancellationToken);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        // Two steps: the database allows one active role per user, so the old assignment must be deactivated
        // before the new one is activated or inserted.
        foreach (var relation in existingRelations.Where(r => !requestedRoleIds.Contains(r.RoleId)))
        {
            relation.IsActive = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var relation in existingRelations.Where(r => requestedRoleIds.Contains(r.RoleId)))
        {
            relation.IsActive = true;
        }

        var existingRoleIds = existingRelations
            .Select(relation => relation.RoleId)
            .ToHashSet();

        foreach (var roleId in requestedRoleIds)
        {
            if (existingRoleIds.Contains(roleId))
                continue;

            _dbContext.UserRoles.Add(new ApplicationUserRole
            {
                UserId = userId,
                RoleId = roleId,
                IsActive = true
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _userManager.UpdateSecurityStampAsync(
            await FindUserAsync(userId, cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        _audit.Operation(
            "RolesSet", "User", userId,
            $"roleIds=[{string.Join(",", requestedRoleIds.OrderBy(x => x))}]");
    }

    public async Task AddRoleToUserAsync(
        long userId,
        long roleId,
        CancellationToken cancellationToken = default)
    {
        await FindUserAsync(userId, cancellationToken);
        await FindRoleAsync(roleId, cancellationToken);

        var relation = await _dbContext.UserRoles
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.RoleId == roleId,
                cancellationToken);

        if (relation is { IsActive: true })
            return;

        if (await _dbContext.UserRoles.AnyAsync(
                x => x.UserId == userId && x.RoleId != roleId && x.IsActive,
                cancellationToken))
        {
            throw new ConflictException(AlreadyHasRoleMessage, ErrorCodes.UserAlreadyHasRole);
        }

        if (relation is not null)
        {

            relation.IsActive = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            _dbContext.UserRoles.Add(new ApplicationUserRole
            {
                UserId = userId,
                RoleId = roleId,
                IsActive = true
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var user = await FindUserAsync(userId, cancellationToken);

        await _userManager.UpdateSecurityStampAsync(user);

        _audit.Operation("RoleAssigned", "User", userId, $"roleId={roleId}");
    }

    public async Task RemoveRoleFromUserAsync(
        long userId,
        long roleId,
        CancellationToken cancellationToken = default)
    {
        await FindUserAsync(userId, cancellationToken);

        var relation = await _dbContext.UserRoles
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.RoleId == roleId,
                cancellationToken);

        if (relation is null || !relation.IsActive)
            return;

        relation.IsActive = false;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var user = await FindUserAsync(userId, cancellationToken);

        await _userManager.UpdateSecurityStampAsync(user);

        _audit.Operation("RoleRemoved", "User", userId, $"roleId={roleId}");
    }

    // ---------------------------------------------------------
    // Role -> Users
    // ---------------------------------------------------------

    public async Task<PagedResult<UserListItemDto>> GetRoleUsersAsync(
        long roleId,
        RoleUserListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await FindRoleAsync(roleId, cancellationToken);

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query =
            from relation in _dbContext.UserRoles.AsNoTracking()
            join user in _userManager.Users.AsNoTracking()
                on relation.UserId equals user.Id
            where relation.RoleId == roleId
                  && relation.IsActive
            select user;

        if (request.IsActive.HasValue)
        {
            query = query.Where(
                user => user.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(user =>
                (user.UserName != null &&
                 user.UserName.Contains(search)) ||
                user.FirstName.Contains(search) ||
                user.LastName.Contains(search) ||
                user.PersonnelCode.Contains(search) ||
                (user.Email != null &&
                 user.Email.Contains(search)));
        }

        var descending = string.Equals(
            request.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        query = (request.SortBy ?? "id")
            .Trim()
            .ToLowerInvariant() switch
        {
            "username" => descending
                ? query.OrderByDescending(user => user.UserName)
                : query.OrderBy(user => user.UserName),

            "lastname" => descending
                ? query.OrderByDescending(user => user.LastName)
                : query.OrderBy(user => user.LastName),

            _ => descending
                ? query.OrderByDescending(user => user.Id)
                : query.OrderBy(user => user.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserListItemDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PersonnelCode = user.PersonnelCode,
                NationalCode = user.NationalCode,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                SupportingDepartmentId = user.SupportingDepartmentId,
                PostId = user.PostId,
                IsActive = user.IsActive
            })
            .ToListAsync(cancellationToken);

        await UserPostLookup.FillAsync(_dbContext, items, cancellationToken);

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task SetRoleUsersAsync(
        long roleId,
        IReadOnlyCollection<long> userIds,
        CancellationToken cancellationToken = default)
    {
        await FindRoleAsync(roleId, cancellationToken);

        var requestedUserIds = userIds.Distinct().ToHashSet();

        var validUserIds = await _userManager.Users
            .Where(user =>
                requestedUserIds.Contains(user.Id) &&
                user.IsActive)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        if (validUserIds.Count != requestedUserIds.Count)
        {
            throw new BadRequestException(
                "یک یا چند کاربر وجود ندارد یا غیرفعال است.", ErrorCodes.UsersInvalid);
        }

        if (await _dbContext.UserRoles.AnyAsync(
                x => requestedUserIds.Contains(x.UserId) && x.RoleId != roleId && x.IsActive,
                cancellationToken))
        {
            throw new ConflictException(
                "حداقل یکی از کاربران انتخاب‌شده نقش دیگری دارد. هر کاربر فقط یک نقش می‌تواند داشته باشد؛ ابتدا نقش فعلی او را بردارید.",
                ErrorCodes.UserAlreadyHasRole);
        }

        var existingRelations = await _dbContext.UserRoles
            .Where(relation => relation.RoleId == roleId)
            .ToListAsync(cancellationToken);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        foreach (var relation in existingRelations)
        {
            relation.IsActive =
                requestedUserIds.Contains(relation.UserId);
        }

        var existingUserIds = existingRelations
            .Select(relation => relation.UserId)
            .ToHashSet();

        foreach (var userId in requestedUserIds)
        {
            if (existingUserIds.Contains(userId))
                continue;

            _dbContext.UserRoles.Add(new ApplicationUserRole
            {
                UserId = userId,
                RoleId = roleId,
                IsActive = true
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var userId in requestedUserIds
                     .Union(existingRelations.Select(x => x.UserId)))
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(
                    x => x.Id == userId,
                    cancellationToken);

            if (user is not null)
            {
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        _audit.Operation(
            "RoleUsersSet", "Role", roleId,
            $"userIds=[{string.Join(",", requestedUserIds.OrderBy(x => x))}]");
    }

    public async Task AddUserToRoleAsync(
        long roleId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        await AddRoleToUserAsync(
            userId,
            roleId,
            cancellationToken);
    }

    public async Task RemoveUserFromRoleAsync(
        long roleId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        await RemoveRoleFromUserAsync(
            userId,
            roleId,
            cancellationToken);
    }

    // ---------------------------------------------------------
    // Private helpers
    // ---------------------------------------------------------

    private async Task<ApplicationUser> FindUserAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("کاربر یافت نشد.", ErrorCodes.UserNotFound);
        }

        if (!user.IsActive)
        {
            throw new BadRequestException(
                "امکان تغییر نقش کاربر غیرفعال وجود ندارد.", ErrorCodes.UserInactive);
        }

        return user;
    }

    private async Task<ApplicationRole> FindRoleAsync(
        long roleId,
        CancellationToken cancellationToken)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(
                x => x.Id == roleId,
                cancellationToken);

        if (role is null)
        {
            throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);
        }

        if (!role.IsActive)
        {
            throw new BadRequestException(
                "نقش غیرفعال است و قابل انتساب نیست.", ErrorCodes.RoleInactive);
        }

        return role;
    }
}