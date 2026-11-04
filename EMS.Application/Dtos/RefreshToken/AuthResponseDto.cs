using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.RefreshToken
{
    public class AuthResponseDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public long? PostId { get; set; }
        public string? PostCode { get; set; }
        public string? PostTitle { get; set; }
        public IEnumerable<string> Roles { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Permissions { get; set; } = Enumerable.Empty<string>();
        public string AccessToken { get; set; } = null!;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }
}
