using EMS.Domain;
using EMS.Application.Validators.User;
using FluentValidation;
using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Export;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;
using EMS.Application.Extensions;
using EMS.Application.Services.User;
using EMS.Application.Interfaces.Auth;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.Identity;

public sealed partial class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EMSDbContext _dbContext;
    private readonly IAuditEventLogger _audit;
    private readonly IExcelExporter _exporter;
    private readonly IRowAccess _access;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        EMSDbContext dbContext,
        IAuditEventLogger audit,
        IExcelExporter exporter,
        IRowAccess access)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _audit = audit;
        _exporter = exporter;
        _access = access;
    }

    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(
        UserListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateListRequest(request);

        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var projected = BuildUserQuery(request)
            .OrderBy(x => x.Id)
            .Select(x => new UserListItemDto
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PersonnelCode = x.PersonnelCode,
                NationalCode = x.NationalCode,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                PostId = x.PostId,
                IsActive = x.IsActive
            });

        var result = await projected.ToPagedResultAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        if (result.Items.Count == 0)
        {
            return result;
        }

        await UserPostLookup.FillAsync(
            _dbContext,
            result.Items,
            cancellationToken);

        var userIds = result.Items
            .Select(x => x.Id)
            .ToArray();

        var roleRows = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
                  && userRole.IsActive
                  && role.IsActive
            select new
            {
                UserId = userRole.UserId,
                RoleId = role.Id,
                RoleName = role.Name
            }
        ).ToListAsync(cancellationToken);

        var rolesByUserId = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => new UserRoleSummaryDto
                    {
                        Id = x.RoleId,
                        Name = x.RoleName ?? string.Empty
                    })
                    .OrderBy(x => x.Name)
                    .ToList());

        var skillRows = await _dbContext.TechnicianSkills
            .AsNoTracking()
            .Where(x =>
                userIds.Contains(x.TechnicianUserId)
                && x.IsActive
                && x.Skill.IsActive)
            .Select(x => new
            {
                UserId = x.TechnicianUserId,
                SkillId = x.Skill.SkillId,
                Title = x.Skill.Title,
                Code = x.Skill.Code,
                Description = x.Skill.Description,
                IsActive = x.Skill.IsActive
            })
            .ToListAsync(cancellationToken);

        var skillsByUserId = skillRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => new UserSkillSummaryDto
                    {
                        SkillId = x.SkillId,
                        Title = x.Title,
                        Code = x.Code,
                        Description = x.Description,
                        IsActive = x.IsActive
                    })
                    .OrderBy(x => x.Title)
                    .ToList());

        foreach (var user in result.Items)
        {
            if (rolesByUserId.TryGetValue(user.Id, out var roles))
            {
                user.Roles = roles;
            }

            if (skillsByUserId.TryGetValue(user.Id, out var skills))
            {
                user.Skills = skills;
            }
        }

        return result;
    }

    /// <summary>
    /// The Excel file of exactly the users the list shows for the same filters. The rows come from <see cref="BuildUserQuery"/>, the same
    /// source as the list, so whatever limits the list (a future row-level security rule, see <see cref="VisibleUsers"/>) limits the file.
    /// </summary>
    public async Task<ExcelFile> ExportAsync(
        UserListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateListRequest(request);
        return await _exporter.ExportAsync(BuildUserRows(request), UserExport.Definition, cancellationToken);
    }

    private static void ValidateListRequest(UserListRequestDto request)
    {
        if (request.RoleId <= 0 || request.SkillId <= 0)
        {
            throw new BadRequestException(
                "شناسه نقش و مهارت باید مثبت باشد.");
        }
    }

    /// <summary>
    /// The rows of the export: the filtered list query, ordered like the list, flattened with the place of work and the roles and skills.
    /// The soft-delete filter is lifted, as in the list's own lookups, so a deactivated post or department still names where the user works.
    /// </summary>
    private IQueryable<UserRow> BuildUserRows(UserListRequestDto request)
    {
        var roles = ActiveRoleMembers();
        var skills = ActiveSkillMembers();
        var departments =
            from m in _dbContext.UserSupportingDepartments
            join d in _dbContext.SupportingDepartments on m.SupportingDepartmentId equals d.SupportingDepartmentId
            select new { m.UserId, d.Title, CityTitle = d.City.Title, ProvinceTitle = d.City.Province.Title };

        return BuildUserQuery(request)
            .IncludingInactive()
            .OrderBy(x => x.Id)
            .Select(x => new UserRow
            {
                Id = x.Id,
                UserName = x.UserName ?? string.Empty,
                FirstName = x.FirstName,
                LastName = x.LastName,
                PersonnelCode = x.PersonnelCode,
                NationalCode = x.NationalCode,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                PostTitle = x.Post!.Title,
                SupportingDepartmentTitle = string.Join("، ", departments.Where(d => d.UserId == x.Id).OrderBy(d => d.Title).Select(d => d.Title)),
                CityTitle = string.Join("، ", departments.Where(d => d.UserId == x.Id).Select(d => d.CityTitle).Distinct().OrderBy(t => t)),
                ProvinceTitle = string.Join("، ", departments.Where(d => d.UserId == x.Id).Select(d => d.ProvinceTitle).Distinct().OrderBy(t => t)),
                RoleNames = string.Join("، ", roles.Where(r => r.UserId == x.Id).OrderBy(r => r.Name).Select(r => r.Name)),
                SkillTitles = string.Join("، ", skills.Where(r => r.UserId == x.Id).OrderBy(r => r.Name).Select(r => r.Name)),
                IsActive = x.IsActive,
            });
    }

    /// <summary>A user reached by his id must be one the caller may see: 404 otherwise, as for a missing one. Call it BEFORE loading the user.</summary>
    private Task RequireVisibleUserAsync(long id, CancellationToken cancellationToken) =>
        _access.RequireVisibleAsync(_dbContext.Users.AsNoTracking(), u => u.Id == id, "User", id, "کاربر موردنظر پیدا نشد.", cancellationToken, ErrorCodes.UserNotFound);

    public async Task<UserDetailDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        // a user the caller may not see looks exactly like one that does not exist (null = 404)
        var user = await VisibleUsers()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        return user is null
            ? null
            : await LoadDetailAsync(user, cancellationToken);
    }

    public async Task<UserDetailDto> CreateAsync(
        CreateUserDto request,
        CancellationToken cancellationToken = default)
    {
        await new CreateUserDtoValidator().ValidateAndThrowAsync(request, cancellationToken);

        await ValidateAssignmentsAsync(
            request.RoleIds,
            request.SkillIds,
            cancellationToken);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var userName = request.UserName.Trim();

        var existingUser =
            await _userManager.FindByNameAsync(userName);

        if (existingUser is not null)
        {
            throw new ConflictException(
                "این نام کاربری قبلاً ثبت شده است.",
                ErrorCodes.UserNameTaken);
        }

        await EnsureUniqueBusinessFieldsAsync(
            request.PersonnelCode,
            request.NationalCode,
            null,
            cancellationToken);

        var departmentIds = NormalizeDepartmentIds(request.SupportingDepartmentIds);
        await EnsureDepartmentsExistAsync(departmentIds, cancellationToken);

        await EnsurePostAsync(request.PostId, currentPostId: null, cancellationToken);
        RequireDepartmentsVisible(0, departmentIds, departmentIds);

        var user = new ApplicationUser
        {
            UserName = userName,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PersonnelCode = request.PersonnelCode.Trim(),
            NationalCode = NormalizeOptional(request.NationalCode),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            Email = NormalizeOptional(request.Email),
            PostId = request.PostId,
            IsActive = request.IsActive,
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result),
                ErrorCodes.IdentityError);
        }

        await ApplyDepartmentsAsync(user.Id, departmentIds, cancellationToken);

        await ApplyAssignmentsAsync(
            user,
            request.RoleIds,
            request.SkillIds,
            cancellationToken);

        var detail = await LoadDetailAsync(
            user,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _audit.Created("User", user.Id);

        return detail;
    }

    public async Task<UserDetailDto?> UpdateAsync(
        long id,
        UpdateUserDto request,
        CancellationToken cancellationToken = default)
    {
        await new UpdateUserDtoValidator().ValidateAndThrowAsync(request, cancellationToken);

        await ValidateAssignmentsAsync(
            request.RoleIds,
            request.SkillIds,
            cancellationToken);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await RequireVisibleUserAsync(id, cancellationToken);
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.RowVersion) &&
            !string.Equals(
                request.RowVersion,
                user.ConcurrencyStamp,
                StringComparison.Ordinal))
        {
            throw new ConflictException(
                RowVersionToken.ConflictMessage,
                ErrorCodes.ConcurrencyConflict);
        }

        var newUserName = request.UserName.Trim();

        var existingByName =
            await _userManager.FindByNameAsync(newUserName);

        if (existingByName is not null &&
            existingByName.Id != id)
        {
            throw new ConflictException(
                "این نام کاربری قبلاً ثبت شده است.",
                ErrorCodes.UserNameTaken);
        }

        await EnsureUniqueBusinessFieldsAsync(
            request.PersonnelCode,
            request.NationalCode,
            id,
            cancellationToken);

        var departmentIds = NormalizeDepartmentIds(request.SupportingDepartmentIds);
        await EnsureDepartmentsExistAsync(departmentIds, cancellationToken);

        await EnsurePostAsync(request.PostId, user.PostId, cancellationToken);

        user.UserName = newUserName;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PersonnelCode = request.PersonnelCode.Trim();
        user.NationalCode = NormalizeOptional(request.NationalCode);
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.Email = NormalizeOptional(request.Email);
        var currentDepartmentIds = await CurrentDepartmentIdsAsync(user.Id, cancellationToken);
        var changedDepartmentIds = currentDepartmentIds.Except(departmentIds).Concat(departmentIds.Except(currentDepartmentIds)).ToList();
        if (changedDepartmentIds.Count > 0)
            RequireDepartmentsVisible(user.Id, changedDepartmentIds, departmentIds);      // putting a user into (or taking him out of) a department the caller cannot see is refused
        user.PostId = request.PostId;
        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (result.Errors.Any(
                e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            throw new ConflictException(
                RowVersionToken.ConflictMessage,
                ErrorCodes.ConcurrencyConflict);
        }

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result),
                ErrorCodes.IdentityError);
        }

        await ApplyDepartmentsAsync(user.Id, departmentIds, cancellationToken);

        await ApplyAssignmentsAsync(
            user,
            request.RoleIds,
            request.SkillIds,
            cancellationToken);

        if (!user.IsActive)
        {
            _audit.Operation("UserDeactivated", "User", user.Id);

            await RevokeRefreshTokensAsync(
                user.Id,
                cancellationToken);
        }

        var detail = await LoadDetailAsync(
            user,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        _audit.Updated("User", user.Id);

        return detail;
    }

    // Logical deletion: preserve the user and relationships.
    public async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        await RequireVisibleUserAsync(id, cancellationToken);
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.IsActive)
        {
            user.IsActive = false;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new BadRequestException(
                    FormatIdentityErrors(result),
                    ErrorCodes.IdentityError);
            }
        }

        await RevokeRefreshTokensAsync(
            id,
            cancellationToken);

        _audit.Deleted("User", id);

        return true;
    }

    public async Task SetPasswordAsync(
        long id,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        await RequireVisibleUserAsync(id, cancellationToken);
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "کاربر موردنظر پیدا نشد.",
                ErrorCodes.UserNotFound);
        }

        var token =
            await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(
            user,
            token,
            newPassword);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result),
                ErrorCodes.IdentityError);
        }

        await RevokeRefreshTokensAsync(
            id,
            cancellationToken);

        _audit.Operation("PasswordReset", "User", id);
    }

    private async Task EnsureUniqueBusinessFieldsAsync(
        string personnelCode,
        string? nationalCode,
        long? exceptUserId,
        CancellationToken cancellationToken)
    {
        var personnel = UserIdentityRules.Normalize(personnelCode);

        // row-access: a uniqueness check must see EVERY user, not only the ones the caller may see (a hidden duplicate is still a duplicate)
        var personnelQuery = _userManager.Users
            .Where(x => x.PersonnelCode == personnel);

        if (exceptUserId.HasValue)
        {
            personnelQuery = personnelQuery
                .Where(x => x.Id != exceptUserId.Value);
        }

        if (await personnelQuery.AnyAsync(cancellationToken))
        {
            throw new ConflictException(
                UserIdentityRules.PersonnelTaken,
                ErrorCodes.PersonnelCodeTaken);
        }

        var national = UserIdentityRules.NormalizeOptional(nationalCode);

        if (national is not null)
        {
            // row-access: uniqueness over every user (see above)
            var nationalQuery = _userManager.Users
                .Where(x => x.NationalCode == national);

            if (exceptUserId.HasValue)
            {
                nationalQuery = nationalQuery
                    .Where(x => x.Id != exceptUserId.Value);
            }

            if (await nationalQuery.AnyAsync(cancellationToken))
            {
                throw new ConflictException(
                    UserIdentityRules.NationalTaken,
                    ErrorCodes.NationalCodeTaken);
            }
        }
    }

    /// <summary>
    /// The chosen post must exist (404) and be active (400). A user may keep a post that was deactivated after it was given to them, so
    /// the check is skipped when the post is the one the user already has.
    /// </summary>
    private async Task EnsurePostAsync(long? postId, long? currentPostId, CancellationToken cancellationToken)
    {
        if (!postId.HasValue || postId == currentPostId)
            return;

        var post = await _dbContext.Posts.IncludingInactive().AsNoTracking()
            .Where(x => x.PostId == postId.Value)
            .Select(x => new { x.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (post is null)
            throw new NotFoundException("سمت انتخاب‌شده وجود ندارد.", ErrorCodes.ParentNotFound);
        if (!post.IsActive)
            throw new BadRequestException("سمت انتخاب‌شده غیرفعال است.", ErrorCodes.ParentInactive);
    }

    private async Task RevokeRefreshTokensAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            var now = DateTime.UtcNow;

            var tokens = await _dbContext.UserRefreshTokens
                .Where(x =>
                    x.UserId == userId &&
                    x.RevokedDateTime == null &&
                    x.ExpiresDateTime > now)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.RevokedDateTime = now;
            }

            if (tokens.Count == 0)
            {
                return;
            }

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);

                _audit.Operation(
                    "RefreshTokensRevoked",
                    "User",
                    userId,
                    $"count={tokens.Count}");

                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 5)
            {
                // A concurrent request may have revoked some tokens.
                // Detach token entries, then re-read.
                foreach (var entry in _dbContext.ChangeTracker
                             .Entries<EMS.Domain.Entities.UserRefreshToken>()
                             .ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string FormatIdentityErrors(IdentityResult result)
    {
        return string.Join(
            " | ",
            result.Errors.Select(x => x.Description));
    }

    private static UserDetailDto MapDetail(ApplicationUser user)
    {
        return new UserDetailDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PersonnelCode = user.PersonnelCode,
            NationalCode = user.NationalCode,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email,
            PostId = user.PostId,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            CreatedDateTime = user.CreatedDateTime,
            ModifiedDateTime = user.ModifiedDateTime,
            RowVersion = user.ConcurrencyStamp ?? string.Empty
        };
    }
}
