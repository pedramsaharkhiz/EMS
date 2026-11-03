using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;
using EMS.Application.Extensions;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence.Identity.Service;

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

    public async Task<PagedResult<UserGroupDto>> GetGroupedAsync(
        UserListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateListRequest(request, grouped: true);

        var users = BuildUserQuery(request);

        IQueryable<GroupMember> members;

        switch (request.GroupBy!.Trim().ToLowerInvariant())
        {
            case "role":
                members =
                    from user in users
                    join item in ActiveRoleMembers()
                        on user.Id equals item.UserId into matches
                    from item in matches.DefaultIfEmpty()
                    select new GroupMember
                    {
                        UserId = user.Id,
                        Key = item.Key,
                        Name = item.Name ?? "بدون نقش فعال"
                    };
                break;

            case "skill":
                members =
                    from user in users
                    join item in ActiveSkillMembers()
                        on user.Id equals item.UserId into matches
                    from item in matches.DefaultIfEmpty()
                    select new GroupMember
                    {
                        UserId = user.Id,
                        Key = item.Key,
                        Name = item.Name ?? "بدون مهارت فعال"
                    };
                break;

            case "department":
                members =
                    from user in users
                    join department in _dbContext.SupportingDepartments
                            .IncludingInactive()
                        on user.SupportingDepartmentId
                        equals (long?)department.SupportingDepartmentId
                        into matches
                    from department in matches.DefaultIfEmpty()
                    select new GroupMember
                    {
                        UserId = user.Id,
                        Key = user.SupportingDepartmentId,
                        Name = department == null
                            ? "بدون دپارتمان"
                            : department.Title
                    };
                break;

            default: // isActive
                members = users.Select(u => new GroupMember
                {
                    UserId = u.Id,
                    Key = u.IsActive ? 1L : 0L,
                    Name = u.IsActive ? "فعال" : "غیرفعال"
                });
                break;
        }

        var groups = members
            .GroupBy(x => new
            {
                x.Key,
                x.Name
            })
            .Select(g => new UserGroupDto
            {
                Key = g.Key.Key,
                Name = g.Key.Name,
                UserCount = g
                    .Select(x => x.UserId)
                    .Distinct()
                    .Count()
            });

        var descending =
            request.SortDirection.Trim().ToLowerInvariant() is "desc" or "dsc";

        IOrderedQueryable<UserGroupDto> ordered =
            (request.SortBy?.Trim().ToLowerInvariant(), descending) switch
            {
                ("usercount", true) =>
                    groups.OrderByDescending(x => x.UserCount),

                ("usercount", false) =>
                    groups.OrderBy(x => x.UserCount),

                ("key", true) =>
                    groups.OrderByDescending(x => x.Key),

                ("key", false) =>
                    groups.OrderBy(x => x.Key),

                (_, true) =>
                    groups.OrderByDescending(x => x.Name),

                _ =>
                    groups.OrderBy(x => x.Name)
            };

        return await ordered
            .ThenBy(x => x.Key)
            .ToPagedResultAsync(
                Math.Max(1, request.PageNumber),
                Math.Clamp(request.PageSize, 1, 100),
                cancellationToken);
    }

    private static void ValidateListRequest(
        UserListRequestDto request,
        bool grouped)
    {
        var direction = request.SortDirection?
            .Trim()
            .ToLowerInvariant();

        if (direction is not ("asc" or "desc" or "dsc"))
        {
            throw new BadRequestException(
                "SortDirection باید asc یا desc باشد.");
        }

        var group = request.GroupBy?
            .Trim()
            .ToLowerInvariant();

        if (grouped &&
            group is not ("role" or "skill" or "department" or "isactive"))
        {
            throw new FluentValidation.ValidationException(new[]
            {
                new FluentValidation.Results.ValidationFailure("groupBy", "گروه‌بندی باید role، skill، department یا isActive باشد.")
            });
        }

        var sort = request.SortBy?
            .Trim()
            .ToLowerInvariant();

        string[] allowed = grouped
            ? ["name", "key", "usercount"]
            : [
                "id",
                "username",
                "firstname",
                "lastname",
                "fullname",
                "personnelcode",
                "createddatetime",
                "isactive",
                "supportingdepartmentid"
            ];

        if (!string.IsNullOrEmpty(sort) && !allowed.Contains(sort))
        {
            throw new BadRequestException(
                "SortBy برای این نوع خروجی معتبر نیست.");
        }

        if (request.RoleId <= 0 || request.SkillId <= 0)
        {
            throw new BadRequestException(
                "شناسه نقش و مهارت باید مثبت باشد.");
        }
    }

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

    private async Task<UserDetailDto> LoadDetailAsync(
        ApplicationUser user,
        CancellationToken ct)
    {
        var detail = MapDetail(user);

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

    private IQueryable<ApplicationUser> BuildUserQuery(
        UserListRequestDto request)
    {
        var query = _userManager.Users.AsNoTracking();

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
            query = query.Where(x =>
                x.SupportingDepartmentId ==
                request.SupportingDepartmentId.Value);
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
                     x.SupportingDepartmentId == numericSearch))
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
