using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.RefreshToken
{
    public class RefreshTokenDto
    {
        public long UserId { get; set; }
        public string TokenHash { get; set; }
        public string CreatedByIp { get; set; }
        public DateTime ExpiresDateTime { get; set; }
        public bool IsActive { get; set; }
    }
}
