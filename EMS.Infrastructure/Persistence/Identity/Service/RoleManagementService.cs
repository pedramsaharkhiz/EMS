using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Role;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence.Identity.Service;

public sealed class RoleManagementService : IRoleManagementService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly EMSDbContext _dbContext;
    private readonly IAuditEventLogger _audit;

    public RoleManagementService(
        RoleManager<ApplicationRole> roleManager,
        EMSDbContext dbContext,
        IAuditEventLogger audit)
    {
        _roleManager = roleManager;
        _dbContext = dbContext;
        _audit = audit;
    }

    public async Task<PagedResult<RoleListItemDto>> GetPagedAsync(
        RoleListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _roleManager.Roles.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(
                r => r.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(
                r => r.Name != null &&
                     r.Name.Contains(search));
        }

        var descending = string.Equals(
            request.SortDirection,
            "desc",
            StringComparison.OrdinalIgnoreCase);

        query = (request.SortBy ?? "id")
            .Trim()
            .ToLowerInvariant() switch
        {
            "name" => descending
                ? query.OrderByDescending(r => r.Name)
                : query.OrderBy(r => r.Name),

            "isactive" => descending
                ? query.OrderByDescending(r => r.IsActive)
                : query.OrderBy(r => r.IsActive),

            _ => descending
                ? query.OrderByDescending(r => r.Id)
                : query.OrderBy(r => r.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleListItemDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                IsActive = r.IsActive,
                UserCount = _dbContext.UserRoles.Count(
                    ur => ur.RoleId == r.Id && ur.IsActive)
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RoleListItemDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<RoleDetailDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);

        if (role is null)
            return null;

        return await MapAsync(role, cancellationToken);
    }

    public async Task<RoleDetailDto> CreateAsync(
        CreateRoleDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "نام نقش الزامی است.");
        }

        var name = request.Name.Trim();

        if (await _roleManager.FindByNameAsync(name) is not null)
        {
            throw new ConflictException(
                "این نقش قبلاً ثبت شده است.");
        }

        var role = new ApplicationRole
        {
            Name = name,
            IsActive = true
        };

        var result = await _roleManager.CreateAsync(role);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result));
        }

        _audit.Created("Role", role.Id);

        return await MapAsync(role, cancellationToken);
    }

    public async Task<RoleDetailDto?> UpdateAsync(
        long id,
        UpdateRoleDto request,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);

        if (role is null)
            return null;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException(
                "نام نقش الزامی است.");
        }

        var name = request.Name.Trim();
        var existingRole =
            await _roleManager.FindByNameAsync(name);

        if (existingRole is not null &&
            existingRole.Id != id)
        {
            throw new ConflictException(
                "این نام نقش قبلاً ثبت شده است.");
        }

        role.Name = name;
        role.IsActive = request.IsActive;

        var result = await _roleManager.UpdateAsync(role);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result));
        }

        _audit.Updated("Role", role.Id);

        return await MapAsync(role, cancellationToken);
    }

    public async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.Roles
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);

        if (role is null)
            return false;

        var hasAssignments = await _dbContext.UserRoles
            .AnyAsync(
                x => x.RoleId == id && x.IsActive,
                cancellationToken);

        if (hasAssignments)
        {
            throw new ConflictException(
                "این نقش هنوز به کاربران اختصاص دارد. " +
                "ابتدا انتساب‌های آن را حذف کنید یا نقش را غیرفعال کنید.");
        }

        var result = await _roleManager.DeleteAsync(role);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result));
        }

        _audit.Deleted("Role", id);

        return true;
    }

    private async Task<RoleDetailDto> MapAsync(
        ApplicationRole role,
        CancellationToken cancellationToken)
    {
        var userCount = await _dbContext.UserRoles
            .CountAsync(
                ur => ur.RoleId == role.Id && ur.IsActive,
                cancellationToken);

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            IsActive = role.IsActive,
            CreatedDateTime = role.CreatedDateTime,
            ModifiedDateTime = role.ModifiedDateTime,
            UserCount = userCount
        };
    }

    private static string FormatIdentityErrors(
        IdentityResult result)
    {
        return string.Join(
            " | ",
            result.Errors.Select(e => e.Description));
    }
}