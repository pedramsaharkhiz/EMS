using EMS.Application.Common.Security;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Identity;
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

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
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
                throw new Exception(string.Join(" | ", roleResult.Errors.Select(x => x.Description)));
        }

        var existingClaims = await _roleManager.GetClaimsAsync(adminRole);
        foreach (var permission in Permissions.All)
        {
            if (!existingClaims.Any(x => x.Type == "permission" && x.Value == permission))
            {
                var claimResult = await _roleManager.AddClaimAsync(adminRole, new Claim("permission", permission));
                if (!claimResult.Succeeded)
                    throw new Exception(string.Join(" | ", claimResult.Errors.Select(x => x.Description)));
            }
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
                throw new Exception(string.Join(" | ", createResult.Errors.Select(x => x.Description)));
        }

        var isInRole = await _userManager.IsInRoleAsync(adminUser, adminRoleName);
        if (!isInRole)
        {
            var addToRoleResult = await _userManager.AddToRoleAsync(adminUser, adminRoleName);
            if (!addToRoleResult.Succeeded)
                throw new Exception(string.Join(" | ", addToRoleResult.Errors.Select(x => x.Description)));
        }
    }
}