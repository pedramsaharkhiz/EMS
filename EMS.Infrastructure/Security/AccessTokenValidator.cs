using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Application.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Security
{
    public sealed class AccessTokenValidator : IAccessTokenValidator
    {
        private readonly EMSDbContext _dbContext;

        public AccessTokenValidator(EMSDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AccessTokenValidationResult> ValidateAsync(long userId, string? tokenSecurityStamp, CancellationToken cancellationToken = default) =>
            (await ValidateWithScopeAsync(userId, tokenSecurityStamp, cancellationToken)).Result;

        public async Task<(AccessTokenValidationResult Result, long? SupportingDepartmentId)> ValidateWithScopeAsync(long userId, string? tokenSecurityStamp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tokenSecurityStamp))
                return (AccessTokenValidationResult.MissingStamp, null);

            // One indexed primary-key lookup, no tracking.
            var user = await _dbContext.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.SecurityStamp, u.SupportingDepartmentId })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null || !user.IsActive)
                return (AccessTokenValidationResult.UserNotFoundOrInactive, null);

            var result = string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal)
                ? AccessTokenValidationResult.Valid
                : AccessTokenValidationResult.StampMismatch;
            return (result, user.SupportingDepartmentId);
        }
    }
}
