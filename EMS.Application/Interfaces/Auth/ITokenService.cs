using EMS.Application.Common.Security;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.Auth
{
    public interface ITokenService
    {
        Task<(string Token, DateTime ExpiresAt)> GenerateAccessTokenAsync(TokenUserDto user); 
        string GenerateRefreshToken();
    }
}
