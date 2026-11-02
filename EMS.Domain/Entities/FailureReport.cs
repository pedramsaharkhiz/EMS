using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class FailureReport : BaseEntity, IAuditableEntity
    {
        public long FailureReportId { get; set; }
        public long EquipmentId { get; set; }
        public long FailureTypeId { get; set; }
        public long ReportTypeId { get; set; }
        public long OrganizationId { get; set; }
        public long? EmployeeId { get; set; }
        public string? ReferenceNumber { get; set; }
        public int PriorityId { get; set; }
        public DateTime ReportedDateTime { get; set; }
        public string? Description { get; set; }

        public Equipment Equipment { get; set; } = null!;
        public FailureType FailureType { get; set; } = null!;
        public ReportType ReportType { get; set; } = null!;
        public Organization Organization { get; set; } = null!;
        public Employee? Employee { get; set; }

        public ICollection<TechnicianAssignment> TechnicianAssignments { get; set; } = new List<TechnicianAssignment>();
        public ICollection<FailureReportAction> FailureReportActions { get; set; } = new List<FailureReportAction>();
    }
}