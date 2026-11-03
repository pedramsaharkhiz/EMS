using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.TechnicianAssignment
{
    public class TechnicianAssignmentDto
    {
        public long FailureReportId { get; set; }
        public long TechnicianAssignmentStatusId { get; set; }
        public long TechnicianUserId { get; set; }
        public long AssignedByUserId { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
