using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class ReportType : BaseEntity, IAuditableEntity
    {
        public long ReportTypeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code  { get; set; }
        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
    }
}
