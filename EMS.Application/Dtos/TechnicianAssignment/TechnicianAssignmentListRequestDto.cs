using EMS.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>
    /// Id filters accept several values: <c>?statusId=1&amp;statusId=4</c>. Everything is optional and combined with AND.
    /// <c>overdue=true</c>: open with a deadline in the past; <c>overdue=false</c>: everything else.
    /// Sort with <c>sortBy</c>: assignedDateTime, deadlineDateTime, completedDateTime, status, reportReference, createdDateTime (default: id).
    /// </summary>
    public class TechnicianAssignmentListRequestDto : PagedRequest
    {
        /// <summary>Looks in the report's reference number, the equipment's Yaas code and the assigner's note.</summary>
        public string? Search { get; set; }
        [FromQuery(Name = "failureReportId")] public long[]? FailureReportIds { get; set; }
        [FromQuery(Name = "technicianUserId")] public long[]? TechnicianUserIds { get; set; }
        [FromQuery(Name = "assignedByUserId")] public long[]? AssignedByUserIds { get; set; }
        [FromQuery(Name = "statusId")] public long[]? StatusIds { get; set; }
        public bool? Overdue { get; set; }
        /// <summary>Assigned at or after (UTC, ISO 8601).</summary>
        public DateTime? AssignedFrom { get; set; }
        /// <summary>Assigned at or before (UTC, ISO 8601).</summary>
        public DateTime? AssignedTo { get; set; }
    }
}
