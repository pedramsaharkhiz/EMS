using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using Azure.Core;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Domain.Entities;
using EMS.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using static EMS.Application.Common.Security.Permissions;
using System.Security.Cryptography;

namespace EMS.Infrastructure.Persistence.Identity.Service
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly EMSDbContext _dbContext;
        private readonly JwtOptions _jwtOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISecurityEventLogger _securityLog;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<ApplicationRole> roleManager, ITokenService tokenService, EMSDbContext dbContext, IOptions<JwtOptions> jwtOptions, IHttpContextAccessor httpContextAccessor, ISecurityEventLogger securityLog)
        {
            _securityLog = securityLog;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _dbContext = dbContext;
            _jwtOptions = jwtOptions.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == request.UserName, cancellationToken);
            // The client always receives the same generic message (no user enumeration);
            // the precise reason is only written to the security log.
            if (user is null)
            {
                _securityLog.LoginFailed(request.UserName, LoginFailureReason.UnknownUser);
                throw new UnauthorizedAccessException("نام کاربری یا کلمه عبور نامعتبر است.");
            }

            if (!user.IsActive)
            {
                _securityLog.LoginFailed(request.UserName, LoginFailureReason.InactiveUser, user.Id);
                throw new UnauthorizedAccessException("نام کاربری یا کلمه عبور نامعتبر است.");
            }

            var signInResult = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
            {
                var lockedOut = signInResult.IsLockedOut || await _userManager.IsLockedOutAsync(user);
                _securityLog.LoginFailed(
                    request.UserName,
                    lockedOut ? LoginFailureReason.LockedOut : LoginFailureReason.InvalidPassword,
                    user.Id);
                throw new UnauthorizedAccessException("نام کاربری یا کلمه عبور نامعتبر است.");
            }

            var response = await BuildAuthResponseAsync(user, cancellationToken);
            _securityLog.LoginSucceeded(user.Id, user.UserName ?? request.UserName);
            return response;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
        {
            var refreshTokenHash = ComputeSha256Hash(request.RefreshToken);
            var tokenEntity = await _dbContext.UserRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

            if (tokenEntity is null || !tokenEntity.IsActive || !tokenEntity.IsValid)
            {
                // A token that was already revoked being presented again is a strong signal of replay/theft.
                var reason = tokenEntity switch
                {
                    null => RefreshRejectionReason.TokenNotFound,
                    { IsRevoked: true } => RefreshRejectionReason.TokenReuseDetected,
                    { IsExpired: true } => RefreshRejectionReason.TokenExpired,
                    _ => RefreshRejectionReason.TokenInactive
                };
                _securityLog.RefreshRejected(reason, tokenEntity?.UserId);
                throw new UnauthorizedAccessException("رفرش توکن نامعتبر است.");
            }

            var user = await _userManager.FindByIdAsync(tokenEntity.UserId.ToString());
            if (user is null || !user.IsActive)
            {
                _securityLog.RefreshRejected(RefreshRejectionReason.UserInvalid, tokenEntity.UserId);
                throw new UnauthorizedAccessException("کاربر نامعتبر است.");
            }

            var now = DateTime.UtcNow;
            tokenEntity.RevokedDateTime = now;
            tokenEntity.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var newRefreshToken = _tokenService.GenerateRefreshToken();
            var newTokenEntity = new UserRefreshToken
            {
                UserId = user.Id,
                TokenHash = ComputeSha256Hash(newRefreshToken),
                CreatedDateTime = now,
                ExpiresDateTime = now.AddDays(_jwtOptions.RefreshTokenExpirationDays),
                CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                CreatedByUserId = user.Id
            };

            _dbContext.UserRefreshTokens.Add(newTokenEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var (roles, permissions) = await GetActiveAccessAsync(user.Id, cancellationToken);
            var tokenUser = new TokenUserDto(Id: user.Id, UserName: user.UserName ?? string.Empty, Email: user.Email ?? string.Empty, Roles: roles.ToList(), Permissions: permissions.ToList(), FirstName: user.FirstName, LastName: user.LastName, SecurityStamp: user.SecurityStamp);
            var accessTokenResult = await _tokenService.GenerateAccessTokenAsync(tokenUser);

            _securityLog.RefreshSucceeded(user.Id);

            return new AuthResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiresAt = accessTokenResult.ExpiresAt,
                RefreshToken = newRefreshToken,
                RefreshTokenExpiresAt = newTokenEntity.ExpiresDateTime
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            var refreshTokenHash = ComputeSha256Hash(refreshToken);
            var tokenEntity = await _dbContext.UserRefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);
            if (tokenEntity is null)
                return;

            tokenEntity.RevokedDateTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _securityLog.RefreshTokenRevoked(tokenEntity.UserId);
        }

        public async Task<long> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default)
        {
            var exists = await _userManager.Users.AnyAsync(x => x.UserName == request.UserName, cancellationToken);
            if (exists)
                throw new ConflictException("کاربری با این نام کاربری قبلاً ثبت شده است.");

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                NormalizedUserName = request.UserName.ToUpperInvariant(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PersonnelCode = request.PersonnelCode,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" | ", result.Errors.Select(x => x.Description)));

            _securityLog.UserRegistered(user.Id, request.UserName);
            return user.Id;
        }

        private static string ComputeSha256Hash(string value)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(value);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var (roles, permissions) = await GetActiveAccessAsync(user.Id, cancellationToken);
            var tokenUser = new TokenUserDto(Id: user.Id, UserName: user.UserName ?? string.Empty, Email: user.Email ?? string.Empty, Roles: roles.ToList(), Permissions: permissions.ToList(), FirstName: user.FirstName, LastName: user.LastName, SecurityStamp: user.SecurityStamp);
            var accessTokenResult = await _tokenService.GenerateAccessTokenAsync(tokenUser);

            var now = DateTime.UtcNow;
            var activeTokens = await _dbContext.UserRefreshTokens.Where(x => x.UserId == user.Id && x.RevokedDateTime == null && x.ExpiresDateTime > now).ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
            {
                token.RevokedDateTime = now;
                token.RevokedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            }

            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpirationDays);
            var refreshTokenEntity = new UserRefreshToken
            {
                UserId = user.Id,
                TokenHash = ComputeSha256Hash(refreshToken),
                CreatedDateTime = now,
                ExpiresDateTime = refreshTokenExpiresAt,
                CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                CreatedByUserId = user.Id
            };

            _dbContext.UserRefreshTokens.Add(refreshTokenEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new AuthResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Roles = roles.ToList(),
                Permissions = permissions.ToList(),
                AccessToken = accessTokenResult.Token,
                AccessTokenExpiresAt = accessTokenResult.ExpiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };
        }

        /// <summary>
        /// Roles and permissions the user effectively holds right now. Only active assignments of active roles count:
        /// removing a role from a user or deactivating a role is a soft change (IsActive = false), and
        /// UserManager.GetRolesAsync would still report such roles.
        /// </summary>
        private async Task<(List<string> Roles, List<string> Permissions)> GetActiveAccessAsync(long userId, CancellationToken cancellationToken)
        {
            var activeRoles = await (
                from userRole in _dbContext.UserRoles
                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                where userRole.UserId == userId && userRole.IsActive && role.IsActive
                select new { role.Id, role.Name })
                .ToListAsync(cancellationToken);

            var roleIds = activeRoles.Select(r => r.Id).ToList();
            var permissions = roleIds.Count == 0
                ? new List<string>()
                : await _dbContext.RoleClaims
                    .Where(c => roleIds.Contains(c.RoleId) && c.ClaimType == "permission" && c.ClaimValue != null)
                    .Select(c => c.ClaimValue!)
                    .Distinct()
                    .ToListAsync(cancellationToken);

            var roleNames = activeRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => r.Name!)
                .Distinct()
                .ToList();

            return (roleNames, permissions);
        }
    }
}