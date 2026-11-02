using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence.Identity.Service;

public sealed class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly EMSDbContext _dbContext;
    private readonly IAuditEventLogger _audit;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        EMSDbContext dbContext,
        IAuditEventLogger audit)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _audit = audit;
    }

    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(UserListRequestDto request, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(1, request.PageNumber); var pageSize = Math.Clamp(request.PageSize, 1, 100); var query = _userManager.Users.AsNoTracking();

        // =========================================
        // 1. فیلتر شناسه کاربر
        // =========================================
        if (request.Id.HasValue)
        {
            query = query.Where(x => x.Id == request.Id.Value);
        }

        // =========================================
        // 2. فیلتر نام کاربری
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var userName = request.UserName.Trim();

            query = query.Where(x =>
                x.UserName != null &&
                x.UserName.Contains(userName));
        }

        // =========================================
        // 3. فیلتر نام
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var firstName = request.FirstName.Trim();

            query = query.Where(x =>
                x.FirstName.Contains(firstName));
        }

        // =========================================
        // 4. فیلتر نام خانوادگی
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var lastName = request.LastName.Trim();

            query = query.Where(x =>
                x.LastName.Contains(lastName));
        }

        // =========================================
        // 5. فیلتر کد پرسنلی
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.PersonnelCode))
        {
            var personnelCode = request.PersonnelCode.Trim();

            query = query.Where(x =>
                x.PersonnelCode.Contains(personnelCode));
        }

        // =========================================
        // 6. فیلتر کد ملی
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.NationalCode))
        {
            var nationalCode = request.NationalCode.Trim();

            query = query.Where(x =>
                x.NationalCode != null &&
                x.NationalCode.Contains(nationalCode));
        }

        // =========================================
        // 7. فیلتر شماره تلفن
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneNumber = request.PhoneNumber.Trim();

            query = query.Where(x =>
                x.PhoneNumber != null &&
                x.PhoneNumber.Contains(phoneNumber));
        }

        // =========================================
        // 8. فیلتر ایمیل
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();

            query = query.Where(x =>
                x.Email != null &&
                x.Email.Contains(email));
        }

        // =========================================
        // 9. فیلتر دپارتمان
        // =========================================
        if (request.SupportingDepartmentId.HasValue)
        {
            query = query.Where(x =>
                x.SupportingDepartmentId ==
                request.SupportingDepartmentId.Value);
        }

        // =========================================
        // 10. فیلتر وضعیت فعال بودن کاربر
        // =========================================
        if (request.IsActive.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == request.IsActive.Value);
        }

        // =========================================
        // 11. جست‌وجوی عمومی
        // =========================================
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            var isNumeric = long.TryParse(search, out var numericSearch);

            query = query.Where(x =>
                (x.UserName != null &&
                 x.UserName.Contains(search))

                || x.FirstName.Contains(search)

                || x.LastName.Contains(search)

                || (x.FirstName + " " + x.LastName).Contains(search)

                || x.PersonnelCode.Contains(search)

                || (x.NationalCode != null &&
                    x.NationalCode.Contains(search))

                || (x.PhoneNumber != null &&
                    x.PhoneNumber.Contains(search))

                || (x.Email != null &&
                    x.Email.Contains(search))

                || (isNumeric &&
                    (x.Id == numericSearch ||
                     x.SupportingDepartmentId == numericSearch))
            );
        }

        // =========================================
        // 12. مرتب‌سازی
        // =========================================
        var sortBy = request.SortBy?.Trim().ToLowerInvariant();

        var descending =
            request.SortDirection?.Trim().ToLowerInvariant() == "desc";

        query = (sortBy, descending) switch
        {
            ("id", true) =>
                query.OrderByDescending(x => x.Id),

            ("id", false) =>
                query.OrderBy(x => x.Id),

            ("username", true) =>
                query.OrderByDescending(x => x.UserName),

            ("username", false) =>
                query.OrderBy(x => x.UserName),

            ("firstname", true) =>
                query.OrderByDescending(x => x.FirstName),

            ("firstname", false) =>
                query.OrderBy(x => x.FirstName),

            ("lastname", true) =>
                query.OrderByDescending(x => x.LastName),

            ("lastname", false) =>
                query.OrderBy(x => x.LastName),

            ("personnelcode", true) =>
                query.OrderByDescending(x => x.PersonnelCode),

            ("personnelcode", false) =>
                query.OrderBy(x => x.PersonnelCode),

            ("createddatetime", true) =>
                query.OrderByDescending(x => x.CreatedDateTime),

            ("createddatetime", false) =>
                query.OrderBy(x => x.CreatedDateTime),

            (_, true) =>
                query.OrderByDescending(x => x.Id),

            _ =>
                query.OrderBy(x => x.Id)
        };

        // =========================================
        // 13. تبدیل اطلاعات اصلی کاربر به DTO
        // =========================================
        var projected = query.Select(x => new UserListItemDto
        {
            Id = x.Id,
            UserName = x.UserName ?? string.Empty,
            FirstName = x.FirstName,
            LastName = x.LastName,
            PersonnelCode = x.PersonnelCode,
            NationalCode = x.NationalCode,
            PhoneNumber = x.PhoneNumber,
            Email = x.Email,
            SupportingDepartmentId = x.SupportingDepartmentId,
            IsActive = x.IsActive
        });

        // =========================================
        // 14. صفحه‌بندی
        // =========================================
        var result = await projected.ToPagedResultAsync(
            pageNumber,
            pageSize,
            cancellationToken);

        // شناسه کاربران صفحه جاری
        var userIds = result.Items
            .Select(x => x.Id)
            .ToArray();

        // اگر صفحه خالی باشد، Queryهای اضافی اجرا نشوند
        if (userIds.Length == 0)
        {
            return result;
        }

        // =========================================
        // 15. دریافت نقش‌های کاربران
        // =========================================
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

        // =========================================
        // 16. دریافت مهارت‌های کاربران
        // =========================================
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

        // =========================================
        // 17. افزودن نقش‌ها و مهارت‌ها به کاربران
        // =========================================
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

    public async Task<UserDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        return user is null ? null : MapDetail(user);
    }

    public async Task<UserDetailDto> CreateAsync(CreateUserDto request, CancellationToken cancellationToken = default)
    {
        var userName = request.UserName.Trim();

        var existingUser =
            await _userManager.FindByNameAsync(userName);

        if (existingUser is not null)
        {
            throw new ConflictException(
                "این نام کاربری قبلاً ثبت شده است.", ErrorCodes.UserNameTaken);
        }

        await EnsureUniqueBusinessFieldsAsync(
            request.PersonnelCode,
            request.NationalCode,
            null,
            cancellationToken);

        await EnsureDepartmentExistsAsync(
            request.SupportingDepartmentId,
            cancellationToken);

        var user = new ApplicationUser
        {
            UserName = userName,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PersonnelCode = request.PersonnelCode.Trim(),
            NationalCode = NormalizeOptional(request.NationalCode),
            PhoneNumber = NormalizeOptional(request.PhoneNumber),
            Email = NormalizeOptional(request.Email),
            SupportingDepartmentId = request.SupportingDepartmentId,
            IsActive = request.IsActive,
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result), ErrorCodes.IdentityError);
        }

        _audit.Created("User", user.Id);

        return MapDetail(user);
    }

    public async Task<UserDetailDto?> UpdateAsync(long id, UpdateUserDto request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var newUserName = request.UserName.Trim();

        var existingByName =
            await _userManager.FindByNameAsync(newUserName);

        if (existingByName is not null &&
            existingByName.Id != id)
        {
            throw new ConflictException(
                "این نام کاربری قبلاً ثبت شده است.", ErrorCodes.UserNameTaken);
        }

        await EnsureUniqueBusinessFieldsAsync(
            request.PersonnelCode,
            request.NationalCode,
            id,
            cancellationToken);

        await EnsureDepartmentExistsAsync(
            request.SupportingDepartmentId,
            cancellationToken);

        user.UserName = newUserName;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PersonnelCode = request.PersonnelCode.Trim();
        user.NationalCode = NormalizeOptional(request.NationalCode);
        user.PhoneNumber = NormalizeOptional(request.PhoneNumber);
        user.Email = NormalizeOptional(request.Email);
        user.SupportingDepartmentId = request.SupportingDepartmentId;
        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new BadRequestException(
                FormatIdentityErrors(result), ErrorCodes.IdentityError);
        }

        _audit.Updated("User", user.Id);

        if (!user.IsActive)
        {
            _audit.Operation("UserDeactivated", "User", user.Id);

            await RevokeRefreshTokensAsync(
                user.Id,
                cancellationToken);
        }

        return MapDetail(user);
    }

    // حذف منطقی: کاربر از پایگاه داده حذف نمی‌شود؛
    // فقط غیرفعال می‌شود تا روابط Identity و سابقه حفظ شوند.
    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
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
                    FormatIdentityErrors(result), ErrorCodes.IdentityError);
            }
        }

        await RevokeRefreshTokensAsync(
            id,
            cancellationToken);

        _audit.Deleted("User", id);

        return true;
    }

    public async Task SetPasswordAsync(long id, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "کاربر موردنظر پیدا نشد.", ErrorCodes.UserNotFound);
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
                FormatIdentityErrors(result), ErrorCodes.IdentityError);
        }

        await RevokeRefreshTokensAsync(
            id,
            cancellationToken);

        // Only the fact that the password changed is logged, never the password.
        _audit.Operation("PasswordReset", "User", id);
    }

    private async Task EnsureUniqueBusinessFieldsAsync(string personnelCode, string? nationalCode, long? exceptUserId, CancellationToken cancellationToken)
    {
        var personnel = personnelCode.Trim();

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
                "این کد پرسنلی قبلاً ثبت شده است.", ErrorCodes.PersonnelCodeTaken);
        }

        var national = NormalizeOptional(nationalCode);

        if (national is not null)
        {
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
                    "این کد ملی قبلاً ثبت شده است.", ErrorCodes.NationalCodeTaken);
            }
        }
    }

    private async Task EnsureDepartmentExistsAsync(long? departmentId, CancellationToken cancellationToken)
    {
        if (departmentId.HasValue &&
            !await _dbContext.SupportingDepartments.AnyAsync(
                x => x.SupportingDepartmentId == departmentId.Value,
                cancellationToken))
        {
            throw new BadRequestException(
                "دپارتمان پشتیبانی انتخاب‌شده وجود ندارد.", ErrorCodes.DepartmentNotFound);
        }
    }

    private async Task RevokeRefreshTokensAsync(long userId, CancellationToken cancellationToken)
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
                return;

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _audit.Operation("RefreshTokensRevoked", "User", userId, $"count={tokens.Count}");
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < 5)
            {
                // A concurrent request already revoked some of these tokens; re-read and revoke what is left.
                foreach (var entry in _dbContext.ChangeTracker.Entries<EMS.Domain.Entities.UserRefreshToken>().ToList())
                    entry.State = EntityState.Detached;
            }
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string FormatIdentityErrors(
        IdentityResult result)
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
            SupportingDepartmentId = user.SupportingDepartmentId,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumberConfirmed = user.PhoneNumberConfirmed,
            CreatedDateTime = user.CreatedDateTime,
            ModifiedDateTime = user.ModifiedDateTime
        };
    }
}