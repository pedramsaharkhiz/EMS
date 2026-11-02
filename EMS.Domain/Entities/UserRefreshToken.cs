using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class UserRefreshToken : BaseEntity, IAuditableEntity
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresDateTime { get; set; }
        public DateTime? RevokedDateTime { get; set; }
        public string CreatedByIp { get; set; } = null!;
        public string? RevokedByIp { get; set; }
        //public long? User { get; set; } = null!;

        public bool IsRevoked => RevokedDateTime.HasValue;
        public bool IsExpired => DateTime.UtcNow >= ExpiresDateTime;
        public bool IsValid => !IsRevoked && !IsExpired;
    }
}