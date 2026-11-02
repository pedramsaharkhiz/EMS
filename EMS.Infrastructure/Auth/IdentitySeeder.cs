using EMS.Application.Common.Security;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace EMS.Infrastructure.Identity.Seed;

public interface IIdentitySeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
public class IdentitySeeder : IIdentitySeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IdentitySeeder> _logger;
    private readonly ISecurityStampInvalidator _stampInvalidator;

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<IdentitySeeder> logger,
        ISecurityStampInvalidator stampInvalidator)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
        _stampInvalidator = stampInvalidator;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        const string adminRoleName = "Admin";
        const string superAdminUserName = "admin";
        const string superAdminPassword = "Admin@123456";

        var adminRole = await _roleManager.FindByNameAsync(adminRoleName);
        if (adminRole is null)
        {
            adminRole = new ApplicationRole
            {
                Name = adminRoleName,
                NormalizedName = adminRoleName.ToUpperInvariant(),
                IsActive = true,
                CreatedByUserId = 0
            };

            var roleResult = await _roleManager.CreateAsync(adminRole);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Identity seed failed: could not create role {RoleName}: {Errors}",
                    adminRoleName, string.Join(" | ", roleResult.Errors.Select(x => x.Code)));
                throw new Exception(string.Join(" | ", roleResult.Errors.Select(x => x.Description)));
            }

            _logger.LogInformation("Identity seed: created role {RoleName}", adminRoleName);
        }

        // Deactivating a role now really removes its permissions from its users. Never leave the system without
        // an active administrator role, otherwise nobody could manage users and roles any more.
        if (!adminRole.IsActive)
        {
            adminRole.IsActive = true;
            var reactivateResult = await _roleManager.UpdateAsync(adminRole);
            if (!reactivateResult.Succeeded)
            {
                _logger.LogError("Identity seed failed: could not re-activate role {RoleName}: {Errors}",
                    adminRoleName, string.Join(" | ", reactivateResult.Errors.Select(x => x.Code)));
                throw new Exception(string.Join(" | ", reactivateResult.Errors.Select(x => x.Description)));
            }

            await _stampInvalidator.InvalidateRoleUsersAsync(adminRole.Id, cancellationToken);
            _logger.LogWarning("Identity seed: role {RoleName} was inactive and has been re-activated", adminRoleName);
        }

        var existingClaims = await _roleManager.GetClaimsAsync(adminRole);
        var addedPermissions = 0;
        foreach (var permission in Permissions.All)
        {
            if (!existingClaims.Any(x => x.Type == "permission" && x.Value == permission))
            {
                var claimResult = await _roleManager.AddClaimAsync(adminRole, new Claim("permission", permission));
                if (!claimResult.Succeeded)
                {
                    _logger.LogError("Identity seed failed: could not add permission {Permission} to role {RoleName}: {Errors}",
                        permission, adminRoleName, string.Join(" | ", claimResult.Errors.Select(x => x.Code)));
                    throw new Exception(string.Join(" | ", claimResult.Errors.Select(x => x.Description)));
                }
                addedPermissions++;
            }
        }

        if (addedPermissions > 0)
        {
            _logger.LogInformation("Identity seed: added {AddedPermissions} permission(s) to role {RoleName}", addedPermissions, adminRoleName);
            // Administrators already holding a token must pick up the new permissions.
            await _stampInvalidator.InvalidateRoleUsersAsync(adminRole.Id, cancellationToken);
        }

        var adminUser = await _userManager.FindByNameAsync(superAdminUserName);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = superAdminUserName,
                NormalizedUserName = superAdminUserName.ToUpperInvariant(),
                FirstName = "System",
                LastName = "Admin",
                PersonnelCode = "000000",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0,
                IsActive = true,
                CreatedByUserId = 0
            };

            var createResult = await _userManager.CreateAsync(adminUser, superAdminPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Identity seed failed: could not create user {UserName}: {Errors}",
                    superAdminUserName, string.Join(" | ", createResult.Errors.Select(x => x.Code)));
                throw new Exception(string.Join(" | ", createResult.Errors.Select(x => x.Description)));
            }

            _logger.LogWarning(
                "Identity seed: created default administrator {UserName}. Change its password after the first sign-in",
                superAdminUserName);
        }

        var isInRole = await _userManager.IsInRoleAsync(adminUser, adminRoleName);
        if (!isInRole)
        {
            var addToRoleResult = await _userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!addToRoleResult.Succeeded)
            {
                _logger.LogError("Identity seed failed: could not add user {UserName} to role {RoleName}: {Errors}",
                    superAdminUserName, adminRoleName, string.Join(" | ", addToRoleResult.Errors.Select(x => x.Code)));
                throw new Exception(string.Join(" | ", addToRoleResult.Errors.Select(x => x.Description)));
            }

            _logger.LogInformation("Identity seed: added user {UserName} to role {RoleName}", superAdminUserName, adminRoleName);
        }
    }
}