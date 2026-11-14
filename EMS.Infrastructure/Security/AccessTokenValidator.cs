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
            (await ValidateWithScopeAsync(userId, tokenSecurityStamp, loadDepartments: false, cancellationToken: cancellationToken)).Result;

        public async Task<(AccessTokenValidationResult Result, IReadOnlyList<long> SupportingDepartmentIds)> ValidateWithScopeAsync(long userId, string? tokenSecurityStamp, bool loadDepartments = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(tokenSecurityStamp))
                return (AccessTokenValidationResult.MissingStamp, Array.Empty<long>());

            // One indexed primary-key lookup, no tracking.
            var user = await _dbContext.Users.AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new { u.IsActive, u.SecurityStamp })
                .FirstOrDefaultAsync(cancellationToken);

            if (user is null || !user.IsActive)
                return (AccessTokenValidationResult.UserNotFoundOrInactive, Array.Empty<long>());

            if (!string.Equals(user.SecurityStamp, tokenSecurityStamp, StringComparison.Ordinal))
                return (AccessTokenValidationResult.StampMismatch, Array.Empty<long>());

            // The departments of the user (indexed by user id; the key is (user, department)). Only a valid token pays for it.
            IReadOnlyList<long> departments = loadDepartments
                ? await _dbContext.UserSupportingDepartments.AsNoTracking()
                    .Where(d => d.UserId == userId).Select(d => d.SupportingDepartmentId).OrderBy(id => id).ToListAsync(cancellationToken)
                : Array.Empty<long>();
            return (AccessTokenValidationResult.Valid, departments);
        }
    }
}
