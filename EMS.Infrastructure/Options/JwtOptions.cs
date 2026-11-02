using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Options
{
    public class JwtOptions
    { 
        public const string SectionName = "Jwt";
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public int AccessTokenExpirationMinutes { get; set; }
        public int RefreshTokenExpirationDays { get; set; } 
    }
}
