using EMS.Application.Dtos.RefreshToken;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.Auth
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
        Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
        /// <summary>Who the user is and what he may do right now, read from the database (401 when the user no longer exists or is inactive).</summary>
        Task<EMS.Application.Dtos.Permission.CurrentUserDto> GetCurrentUserAsync(long userId, CancellationToken cancellationToken = default);
        Task<long> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default);
    }
}
