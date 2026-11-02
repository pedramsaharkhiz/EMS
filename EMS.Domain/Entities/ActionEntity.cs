using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class ActionEntity : BaseEntity, IAuditableEntity
    {
        public long ActionId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public ICollection<TechnicianAssignmentAction> TechnicianAssignmentActions { get; set; } = new List<TechnicianAssignmentAction>();
        public ICollection<FailureReportAction> FailureReportActions { get; set; } = new List<FailureReportAction>();
    }
}