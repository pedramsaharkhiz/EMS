using EMS.Infrastructure.Persistence.Identity;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;
using EMS.Application.Extensions;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.Identity;

public sealed partial class UserManagementService
{
    private sealed class GroupMember
    {
        public long UserId { get; set; }

        public long? Key { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private IQueryable<GroupMember> ActiveRoleMembers() =>
        from relation in _dbContext.UserRoles.AsNoTracking()
        join role in _dbContext.Roles.AsNoTracking()
            on relation.RoleId equals role.Id
        where relation.IsActive && role.IsActive
        select new GroupMember
        {
            UserId = relation.UserId,
            Key = role.Id,
            Name = role.Name ?? ""
        };

    private IQueryable<GroupMember> ActiveSkillMembers() =>
        from relation in _dbContext.TechnicianSkills.AsNoTracking()
        where relation.IsActive && relation.Skill.IsActive
        select new GroupMember
        {
            UserId = relation.TechnicianUserId,
            Key = relation.SkillId,
            Name = relation.Skill.Title
        };




    private async Task ValidateAssignmentsAsync(
        List<long>? roleIds,
        List<long>? skillIds,
        CancellationToken ct)
    {
        static bool Invalid(List<long>? ids) =>
            ids != null &&
            (
                ids.Count > 100 ||
                ids.Any(x => x <= 0) ||
                ids.Distinct().Count() != ids.Count
            );

        if (roleIds is { Count: > 1 })
        {
            throw new FluentValidation.ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("roleIds", "هر کاربر فقط یک نقش می‌تواند داشته باشد.")
            });
        }

        if (Invalid(roleIds) || Invalid(skillIds))
        {
            throw new BadRequestException(
                "شناسه‌ها باید مثبت، بدون تکرار و حداکثر ۱۰۰ مورد باشند.");
        }

        if (roleIds != null &&
            await _dbContext.Roles.CountAsync(
                x => roleIds.Contains(x.Id) && x.IsActive,
                ct) != roleIds.Count)
        {
            throw new BadRequestException(
                "یک یا چند نقش وجود ندارد یا غیرفعال است.",
                ErrorCodes.RolesInvalid);
        }

        if (skillIds != null &&
            await _dbContext.Set<Skill>().CountAsync(
                x => skillIds.Contains(x.SkillId) && x.IsActive,
                ct) != skillIds.Count)
        {
            throw new BadRequestException(
                "یک یا چند مهارت وجود ندارد یا غیرفعال است.", ErrorCodes.SkillsInvalid);
        }
    }

    // Must run inside the surrounding Create/Update transaction.
    private async Task ApplyAssignmentsAsync(
        ApplicationUser user,
        List<long>? roleIds,
        List<long>? skillIds,
        CancellationToken ct)
    {
        var rolesChanged = false;

        if (roleIds != null)
        {
            var relations = await _dbContext.UserRoles
                .Where(x => x.UserId == user.Id)
                .ToListAsync(ct);

            // One active role per user (unique filtered index): deactivate the old assignment and save
            // before the new one is activated or inserted.
            foreach (var relation in relations.Where(r => r.IsActive && !roleIds.Contains(r.RoleId)))
            {
                rolesChanged = true;
                relation.IsActive = false;
            }

            await _dbContext.SaveChangesAsync(ct);

            foreach (var relation in relations.Where(r => !r.IsActive && roleIds.Contains(r.RoleId)))
            {
                rolesChanged = true;
                relation.IsActive = true;
            }

            var existingIds = relations
                .Select(x => x.RoleId)
                .ToHashSet();

            foreach (var id in roleIds.Where(
                         x => !existingIds.Contains(x)))
            {
                rolesChanged = true;

                _dbContext.UserRoles.Add(new ApplicationUserRole
                {
                    UserId = user.Id,
                    RoleId = id,
                    IsActive = true
                });
            }
        }

        if (skillIds != null)
        {
            // Include inactive relationships to restore instead of duplicating.
            var relations = await _dbContext.TechnicianSkills
                .IncludingInactive()
                .Where(x => x.TechnicianUserId == user.Id)
                .ToListAsync(ct);

            foreach (var relation in relations)
            {
                relation.IsActive = skillIds.Contains(relation.SkillId);
            }

            var existingIds = relations
                .Select(x => x.SkillId)
                .ToHashSet();

            foreach (var id in skillIds.Where(
                         x => !existingIds.Contains(x)))
            {
                _dbContext.TechnicianSkills.Add(new TechnicianSkill
                {
                    TechnicianUserId = user.Id,
                    SkillId = id,
                    IsActive = true
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        if (rolesChanged)
        {
            var result = await _userManager.UpdateSecurityStampAsync(user);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(
                        x => x.Code ==
                             nameof(IdentityErrorDescriber.ConcurrencyFailure)))
                {
                    throw new ConflictException(
                        RowVersionToken.ConflictMessage,
                        ErrorCodes.ConcurrencyConflict);
                }

                throw new BadRequestException(
                    FormatIdentityErrors(result),
                    ErrorCodes.IdentityError);
            }

            await RevokeRefreshTokensAsync(user.Id, ct);
        }
    }

    // ------------------------------------------------------------------ supporting departments (a user may work for several)

    /// <summary>The requested departments: distinct ids, in order. A missing list means none.</summary>
    private static List<long> NormalizeDepartmentIds(IEnumerable<long>? ids) => (ids ?? Enumerable.Empty<long>()).Distinct().OrderBy(id => id).ToList();

    /// <summary>Every requested department must exist (active or not: a user keeps a department that was deactivated later).</summary>
    private async Task EnsureDepartmentsExistAsync(IReadOnlyCollection<long> departmentIds, CancellationToken cancellationToken)
    {
        if (departmentIds.Count == 0)
            return;

        var found = await _dbContext.SupportingDepartments.IncludingInactive().AsNoTracking()
            .Where(d => departmentIds.Contains(d.SupportingDepartmentId)).CountAsync(cancellationToken);
        if (found != departmentIds.Count)
            throw new BadRequestException("دپارتمان پشتیبانی انتخاب‌شده وجود ندارد.", ErrorCodes.DepartmentNotFound);
    }

    private static ApplicationUser DepartmentCandidate(long userId, IEnumerable<long> departmentIds) => new()
    {
        Id = userId,
        SupportingDepartments = departmentIds.Select(id => new UserSupportingDepartment { UserId = userId, SupportingDepartmentId = id }).ToList(),
    };

    /// <summary>
    /// What the caller may do with the departments of a user, with the row-security rule applied to users that do not exist yet:
    /// every department that is ADDED or REMOVED must be one the caller sees (404 <c>department_not_found</c> otherwise), and the user must stay visible to the caller
    /// afterwards (a restricted caller cannot leave a user without any department of his own). A department the caller cannot see and does not touch stays as it is.
    /// </summary>
    private void RequireDepartmentsVisible(long userId, IReadOnlyCollection<long> changed, IReadOnlyCollection<long> resulting)
    {
        foreach (var id in changed)
        {
            if (!_access.IsVisible(DepartmentCandidate(0, new[] { id })))
                throw new NotFoundException("دپارتمان پشتیبانی یافت نشد.", ErrorCodes.DepartmentNotFound);
        }

        if (!_access.IsVisible(DepartmentCandidate(userId, resulting)))
            throw new NotFoundException("دپارتمان پشتیبانی یافت نشد.", ErrorCodes.DepartmentNotFound);
    }

    /// <summary>The departments a user has now, ordered by id.</summary>
    private async Task<List<long>> CurrentDepartmentIdsAsync(long userId, CancellationToken cancellationToken) =>
        await _dbContext.UserSupportingDepartments.AsNoTracking().Where(d => d.UserId == userId)
            .Select(d => d.SupportingDepartmentId).OrderBy(id => id).ToListAsync(cancellationToken);

    /// <summary>Makes the user's departments exactly <paramref name="departmentIds"/> (adds the new ones, removes the others). Must run inside the surrounding Create/Update transaction.</summary>
    private async Task ApplyDepartmentsAsync(long userId, IReadOnlyCollection<long> departmentIds, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UserSupportingDepartments.Where(d => d.UserId == userId).ToListAsync(cancellationToken);
        var remove = existing.Where(d => !departmentIds.Contains(d.SupportingDepartmentId)).ToList();
        var add = departmentIds.Where(id => existing.All(d => d.SupportingDepartmentId != id)).ToList();
        if (remove.Count == 0 && add.Count == 0)
            return;

        _dbContext.UserSupportingDepartments.RemoveRange(remove);
        _dbContext.UserSupportingDepartments.AddRange(add.Select(id => new UserSupportingDepartment { UserId = userId, SupportingDepartmentId = id }));
        await _dbContext.SaveChangesAsync(cancellationToken);
        _audit.Operation("UserDepartmentsChanged", "User", userId);
    }

    private async Task<UserDetailDto> LoadDetailAsync(
        ApplicationUser user,
        CancellationToken ct)
    {
        var detail = MapDetail(user);
        await UserPostLookup.FillAsync(_dbContext, new[] { detail }, ct);

        detail.Roles = await ActiveRoleMembers()
            .Where(x => x.UserId == user.Id)
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Key)
            .Select(x => new UserRoleSummaryDto
            {
                Id = x.Key!.Value,
                Name = x.Name
            })
            .ToListAsync(ct);

        detail.Skills = await _dbContext.TechnicianSkills
            .AsNoTracking()
            .Where(x =>
                x.TechnicianUserId == user.Id &&
                x.IsActive &&
                x.Skill.IsActive)
            .OrderBy(x => x.Skill.Title)
            .ThenBy(x => x.SkillId)
            .Select(x => new UserSkillSummaryDto
            {
                SkillId = x.SkillId,
                Title = x.Skill.Title,
                Code = x.Skill.Code,
                Description = x.Skill.Description,
                IsActive = x.Skill.IsActive
            })
            .ToListAsync(ct);

        return detail;
    }

    /// <summary>
    /// THE source of user rows for the list AND the Excel export (they both start from <see cref="BuildUserQuery"/>). Whatever decides which users
    /// a caller may see (a future row-level security rule) belongs here, once. The detail (<c>GetByIdAsync</c>) reads users by id and must follow the same rule.
    /// </summary>
    private IQueryable<ApplicationUser> VisibleUsers() => _access.Apply(_dbContext.Users.AsNoTracking());

    private IQueryable<ApplicationUser> BuildUserQuery(
        UserListRequestDto request)
    {
        var query = VisibleUsers();

        if (request.Id.HasValue)
        {
            query = query.Where(x => x.Id == request.Id.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var userName = request.UserName.Trim();

            query = query.Where(x =>
                x.UserName != null &&
                x.UserName.Contains(userName));
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            var firstName = request.FirstName.Trim();

            query = query.Where(x =>
                x.FirstName.Contains(firstName));
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            var lastName = request.LastName.Trim();

            query = query.Where(x =>
                x.LastName.Contains(lastName));
        }

        if (!string.IsNullOrWhiteSpace(request.PersonnelCode))
        {
            var personnelCode = request.PersonnelCode.Trim();

            query = query.Where(x =>
                x.PersonnelCode.Contains(personnelCode));
        }

        if (!string.IsNullOrWhiteSpace(request.NationalCode))
        {
            var nationalCode = request.NationalCode.Trim();

            query = query.Where(x =>
                x.NationalCode != null &&
                x.NationalCode.Contains(nationalCode));
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneNumber = request.PhoneNumber.Trim();

            query = query.Where(x =>
                x.PhoneNumber != null &&
                x.PhoneNumber.Contains(phoneNumber));
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();

            query = query.Where(x =>
                x.Email != null &&
                x.Email.Contains(email));
        }

        if (request.SupportingDepartmentId.HasValue)
        {
            var departmentId = request.SupportingDepartmentId.Value;
            query = query.Where(x => x.SupportingDepartments.Any(d => d.SupportingDepartmentId == departmentId));
        }

        if (request.PostId.HasValue)
        {
            query = query.Where(x => x.PostId == request.PostId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(x =>
                x.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            var isNumeric = long.TryParse(
                search,
                out var numericSearch);

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
                     x.SupportingDepartments.Any(d => d.SupportingDepartmentId == numericSearch)))
            );
        }

        var roles = ActiveRoleMembers();
        var skills = ActiveSkillMembers();

        if (request.RoleId.HasValue)
        {
            query = query.Where(u =>
                roles.Any(r =>
                    r.UserId == u.Id &&
                    r.Key == request.RoleId));
        }

        if (request.SkillId.HasValue)
        {
            query = query.Where(u =>
                skills.Any(r =>
                    r.UserId == u.Id &&
                    r.Key == request.SkillId));
        }

        if (request.HasRole.HasValue)
        {
            query = query.Where(u =>
                roles.Any(r => r.UserId == u.Id) ==
                request.HasRole.Value);
        }

        if (request.HasSkill.HasValue)
        {
            query = query.Where(u =>
                skills.Any(r => r.UserId == u.Id) ==
                request.HasSkill.Value);
        }

        return query;
    }
}
