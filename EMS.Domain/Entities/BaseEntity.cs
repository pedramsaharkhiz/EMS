using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class BaseEntity
    {
        public bool IsActive { get; set; } = true;

        public long? CreatedByUserId { get; set; }
        public DateTime CreatedDateTime { get; set; }

        public long? ModifiedByUserId { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
