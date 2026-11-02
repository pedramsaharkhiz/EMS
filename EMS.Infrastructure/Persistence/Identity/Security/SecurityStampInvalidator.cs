using EMS.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence.Identity.Security
{
    public sealed class SecurityStampInvalidator : ISecurityStampInvalidator
    {
        private readonly EMSDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;

        public SecurityStampInvalidator(EMSDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task InvalidateRoleUsersAsync(long roleId, CancellationToken cancellationToken = default)
        {
            var userIds = await _dbContext.UserRoles
                .Where(ur => ur.RoleId == roleId && ur.IsActive)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user is not null)
                    await _userManager.UpdateSecurityStampAsync(user);
            }
        }
    }
}
