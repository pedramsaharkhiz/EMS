using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class FailureCategory : BaseEntity, IAuditableEntity
    {
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = null!;
        public ICollection<FailureType> FailureTypes { get; set; } = new List<FailureType>();
    }
}
