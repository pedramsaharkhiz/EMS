using EMS.Application.Common.Security;
using EMS.Infrastructure.Persistence;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Dtos.Permission;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace EMS.Infrastructure.Services.Identity
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IAuditEventLogger _audit;
        private readonly ISecurityStampInvalidator _stampInvalidator;
        public RoleService(RoleManager<ApplicationRole> roleManager, IAuditEventLogger audit, ISecurityStampInvalidator stampInvalidator)
        {
            _roleManager = roleManager;
            _audit = audit;
            _stampInvalidator = stampInvalidator;
        }

        public async Task CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
        {
            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
                throw new ConflictException("نقش قبلاً ثبت شده است.", ErrorCodes.RoleNameTaken);
            var role = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                IsActive = true
            };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(x => x.Description)), ErrorCodes.IdentityError);

            _audit.Created("Role", role.Id);
        }

        public async Task AssignPermissionsAsync(string roleName, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);

            await ReplacePermissionsAsync(role, permissions, cancellationToken);
        }

        public async Task<RolePermissionsDto> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                       ?? throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);
            return await ToRolePermissionsAsync(role);
        }

        public async Task<RolePermissionsDto> SetRolePermissionsAsync(long roleId, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                       ?? throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);

            await ReplacePermissionsAsync(role, permissions, cancellationToken);
            return await ToRolePermissionsAsync(role);
        }

        /// <summary>The one place a role's permissions are written (both the name-based and the id-based endpoint end here).</summary>
        private async Task ReplacePermissionsAsync(ApplicationRole role, IEnumerable<string> permissions, CancellationToken cancellationToken)
        {
            // The administrator role always holds every permission (the start-up seed keeps it so): nobody may narrow it and lock everyone out.
            if (SystemRoles.IsSystem(role.Name))
                throw new ConflictException("دسترسی‌های نقش مدیر سیستم قابل تغییر نیست؛ این نقش همیشه همه‌ی دسترسی‌ها را دارد.", ErrorCodes.ProtectedRecord);

            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var previousPermissions = existingClaims.Where(x => x.Type == "permission").Select(x => x.Value).ToHashSet();
            // Retired permissions (see Permissions.Retired) are accepted from older clients but no longer stored.
            var newPermissions = permissions.Distinct().Where(p => !Permissions.Retired.Contains(p)).ToList();

            foreach (var claim in existingClaims.Where(x => x.Type == "permission" && !newPermissions.Contains(x.Value)))
                await _roleManager.RemoveClaimAsync(role, claim);
            foreach (var permission in newPermissions.Where(p => !previousPermissions.Contains(p)))
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission));

            // Permission changes are security-relevant: record exactly which permissions were granted / revoked.
            var granted = newPermissions.Where(p => !previousPermissions.Contains(p)).OrderBy(p => p).ToList();
            var revoked = previousPermissions.Where(p => !newPermissions.Contains(p)).OrderBy(p => p).ToList();
            if (granted.Count == 0 && revoked.Count == 0)
                return;     // nothing changed: no token is invalidated and nothing is audited

            // Users of this role hold the old permissions in their access tokens: invalidate those tokens.
            await _stampInvalidator.InvalidateRoleUsersAsync(role.Id, cancellationToken);

            _audit.Operation(
                "PermissionsChanged", "Role", role.Id,
                $"total={newPermissions.Count} granted=[{string.Join(",", granted)}] revoked=[{string.Join(",", revoked)}]");
        }

        private async Task<RolePermissionsDto> ToRolePermissionsAsync(ApplicationRole role)
        {
            var held = (await _roleManager.GetClaimsAsync(role)).Where(x => x.Type == "permission").Select(x => x.Value).ToHashSet(StringComparer.Ordinal);
            var groups = PermissionCatalog.Groups.Select(g => new RolePermissionGroupDto
            {
                Key = g.Key,
                Title = g.Title,
                Permissions = g.Permissions.Select(p => new RolePermissionItemDto
                {
                    Name = p.Name, Action = p.Action, Title = p.Title, Description = p.Description, Granted = held.Contains(p.Name),
                }).ToList(),
            }).ToList();

            return new RolePermissionsDto
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                IsActive = role.IsActive,
                IsSystem = SystemRoles.IsSystem(role.Name),
                GrantedCount = groups.Sum(g => g.Permissions.Count(p => p.Granted)),
                TotalCount = groups.Sum(g => g.Permissions.Count),
                Groups = groups,
            };
        }

        public async Task<List<string>> GetPermissionsAsync(string roleName, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);
            var claims = await _roleManager.GetClaimsAsync(role);
            return claims.Where(x => x.Type == "permission" && !Permissions.Retired.Contains(x.Value)).Select(x => x.Value).ToList();
        }
    }
}