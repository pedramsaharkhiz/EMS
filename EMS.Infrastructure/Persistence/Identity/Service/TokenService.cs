using EMS.Application.Common.Security;
using EMS.Application.Interfaces.Auth;
using EMS.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static EMS.Application.Common.Security.Permissions;

namespace EMS.Infrastructure.Persistence.Identity.Service
{
    public class TokenService : ITokenService
    {
        private readonly JwtOptions _jwtOptions;
        private readonly UserManager<ApplicationUser> _userManager;

        public TokenService(IOptions<JwtOptions> jwtOptions, UserManager<ApplicationUser> userManager)
        {
            _jwtOptions = jwtOptions.Value;
            _userManager = userManager;
        }

        public Task<(string Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(TokenUserDto user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty),
                new("firstName", user.FirstName ?? string.Empty),
                new("lastName", user.LastName ?? string.Empty)
            };

            if (user.Roles is not null && user.Roles.Count > 0)
            {
                claims.AddRange(
                    user.Roles
                        .Where(role => !string.IsNullOrWhiteSpace(role))
                        .Distinct()
                        .Select(role => new Claim(ClaimTypes.Role, role)));
            }

            if (user.Permissions is not null && user.Permissions.Count > 0)
            {
                claims.AddRange(
                    user.Permissions
                        .Where(permission => !string.IsNullOrWhiteSpace(permission))
                        .Distinct()
                        .Select(permission => new Claim("permission", permission)));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return Task.FromResult((jwt, expiresAt));
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }
    }
}