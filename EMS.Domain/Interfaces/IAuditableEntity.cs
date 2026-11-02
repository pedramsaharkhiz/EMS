using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IAuditableEntity
    {
        long? CreatedByUserId { get; set; }
        DateTime CreatedDateTime { get; set; }
        long? ModifiedByUserId { get; set; }
        DateTime? ModifiedDateTime { get; set; }
    }
}
