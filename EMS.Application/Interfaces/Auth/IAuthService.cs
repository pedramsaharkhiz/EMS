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
        Task<long> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default);
    }
}
