using EMS.Application.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Persistence.Identity.Security
{
    public sealed class AccessTokenValidator : IAccessTokenValidator
    {
        private readonly EMSDbContext _dbContext;

        public AccessTokenValidator(EMSDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AccessTokenValidationResult> ValidateAsync(long userId, string? tokenSecurityStamp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tokenSecurityStamp))
                return AccessTokenValidationResult.MissingStamp;

            // One indexed primary-key lookup, no tracking.
            var user = await _dbContext.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.SecurityStamp })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null || !user.IsActive)
                return AccessTokenValidationResult.UserNotFoundOrInactive;

            return string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal)
                ? AccessTokenValidationResult.Valid
                : AccessTokenValidationResult.StampMismatch;
        }
    }
}
