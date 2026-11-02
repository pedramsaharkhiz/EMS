using EMS.Application.Common.Exceptions;
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
        public RoleService(RoleManager<ApplicationRole> roleManager)
        {
            _roleManager = roleManager;
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
        }

        public async Task AssignPermissionsAsync(string roleName, IEnumerable<string> permissions, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null)
                throw new NotFoundException("نقش یافت نشد.");
            var existingClaims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in existingClaims.Where(x => x.Type == "permission"))
            {
                await _roleManager.RemoveClaimAsync(role, claim);
            }
            foreach (var permission in permissions.Distinct())
            {
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
            }
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