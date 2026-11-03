using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.FailureReport
{
    public class FailureReportDto
    {
        public long EquipmentId { get; set; }
        public long FailureTypeId { get; set; }
        public long ReportTypeId { get; set; }
        public long OrganizationId { get; set; }
        public long? EmployeeId { get; set; }
        public int PriorityId { get; set; }
        public DateTime ReportedDateTime { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public string ReferenceNumber { get; set; }
    }
}