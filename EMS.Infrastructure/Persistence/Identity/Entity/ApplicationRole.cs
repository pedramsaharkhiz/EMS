using EMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Identity
{
    public class ApplicationRole : IdentityRole<long>, IAuditableEntity
    {
        public bool IsActive { get; set; } = true;
        public long? CreatedByUserId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public long? ModifiedByUserId { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
