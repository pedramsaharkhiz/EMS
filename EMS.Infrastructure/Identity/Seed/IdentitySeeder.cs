using EMS.Application.Common.Security;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<IdentitySeeder> logger,
        ISecurityStampInvalidator stampInvalidator,
        IConfiguration configuration)
    {
        _configuration = configuration;
        _roleManager = roleManager;
        _userManager = userManager;
        _logger = logger;
        _stampInvalidator = stampInvalidator;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        const string adminRoleName = "Admin";
        const string superAdminUserName = "admin";
        // The password of the first administrator is a secret: it comes from the environment (Seed__AdminPassword) or the secret store.
        // There is no built-in default. The value below only exists to RECOGNISE the old published default on a database that still uses it.
        const string legacyPublishedPassword = "Admin@123456";
        var superAdminPassword = _configuration["Seed:AdminPassword"];

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

        // Permissions that no longer exist (see Permissions.Retired) are removed from the administrator role.
        var removedPermissions = 0;
        foreach (var claim in existingClaims.Where(x => x.Type == "permission" && Permissions.Retired.Contains(x.Value)))
        {
            var removeResult = await _roleManager.RemoveClaimAsync(adminRole, claim);
            if (!removeResult.Succeeded)
                throw new Exception(string.Join(" | ", removeResult.Errors.Select(x => x.Description)));
            removedPermissions++;
        }

        if (removedPermissions > 0)
            _logger.LogInformation("Identity seed: removed {RemovedPermissions} retired permission(s) from role {RoleName}", removedPermissions, adminRoleName);

        if (addedPermissions + removedPermissions > 0)
        {
            _logger.LogInformation("Identity seed: added {AddedPermissions} permission(s) to role {RoleName}", addedPermissions, adminRoleName);
            // Administrators already holding a token must pick up the new permissions.
            await _stampInvalidator.InvalidateRoleUsersAsync(adminRole.Id, cancellationToken);
        }

        var adminUser = await _userManager.FindByNameAsync(superAdminUserName);
        if (adminUser is null && string.IsNullOrWhiteSpace(superAdminPassword))
        {
            _logger.LogWarning(
                "Identity seed: the administrator {UserName} does not exist and Seed:AdminPassword is not set, so it was NOT created. "
                + "Set the environment variable Seed__AdminPassword (see docs/SECRETS.md) and restart to create it",
                superAdminUserName);
            return;
        }

        if (adminUser is null)
        {
            if (string.Equals(superAdminPassword, legacyPublishedPassword, StringComparison.Ordinal))
                throw new InvalidOperationException("Seed:AdminPassword is the value that was published in the repository. Choose another password.");

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

            var createResult = await _userManager.CreateAsync(adminUser, superAdminPassword!);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Identity seed failed: could not create user {UserName}: {Errors}",
                    superAdminUserName, string.Join(" | ", createResult.Errors.Select(x => x.Code)));
                throw new Exception(string.Join(" | ", createResult.Errors.Select(x => x.Description)));
            }

            _logger.LogWarning(
                "Identity seed: created the administrator {UserName} with the password from Seed:AdminPassword. Remove that setting after the first start",
                superAdminUserName);
        }
        else if (await _userManager.CheckPasswordAsync(adminUser, legacyPublishedPassword))
        {
            // Not blocked (that could lock the owners out), but impossible to miss in the log.
            _logger.LogCritical(
                "SECURITY: the administrator {UserName} still has the password that was published in the repository. Change it NOW (PUT /api/Users/{{id}}/password).",
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