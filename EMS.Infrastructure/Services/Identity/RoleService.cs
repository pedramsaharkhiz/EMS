using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
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
        public RoleService(RoleManager<ApplicationRole> roleManager, IAuditEventLogger audit)
        {
            _roleManager = roleManager;
            _audit = audit;
        }

        public async Task CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
        {
            var exists = await _roleManager.RoleExistsAsync(roleName);
            if (exists)
                throw new ConflictException("نقش قبلاً ثبت شده است.");
            var role = new ApplicationRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                IsActive = true
            };
            var result = await _roleManager.CreateAsync(role);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(x => x.Description)));

            _audit.Created("Role", role.Id);
        }

        public async Task AssignPermissionsAsync(string roleName, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                throw new NotFoundException("نقش یافت نشد.");
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            var previousPermissions = existingClaims.Where(x => x.Type == "permission").Select(x => x.Value).ToHashSet();
            foreach (var claim in existingClaims.Where(x => x.Type == "permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
            var newPermissions = permissions.Distinct().ToList();
            foreach (var permission in newPermissions)
            {
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }

            // Permission changes are security-relevant: record exactly which permissions were granted / revoked.
            var granted = newPermissions.Where(p => !previousPermissions.Contains(p)).OrderBy(p => p).ToList();
            var revoked = previousPermissions.Where(p => !newPermissions.Contains(p)).OrderBy(p => p).ToList();
            _audit.Operation(
                "PermissionsChanged", "Role", role.Id,
                $"total={newPermissions.Count} granted=[{string.Join(",", granted)}] revoked=[{string.Join(",", revoked)}]");
        }

        public async Task<List<string>> GetPermissionsAsync(string roleName, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                throw new NotFoundException("نقش یافت نشد.");
            var claims = await _roleManager.GetClaimsAsync(role);
            return claims.Where(x => x.Type == "permission").Select(x => x.Value).ToList();
        }
    }
}