using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class FailureType : BaseEntity, IAuditableEntity
    {
        public long FailureTypeId { get; set; }
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public FailureCategory FailureCategory { get; set; } = null!;
        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
    }
}
