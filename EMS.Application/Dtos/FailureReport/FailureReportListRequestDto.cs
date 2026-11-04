using EMS.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>
    /// Id filters accept several values: <c>?statusId=1&amp;statusId=2</c>. Everything is optional and combined with AND.
    /// Sort with <c>sortBy</c>: referenceNumber, equipment, failureType, failureCategory, reportType, status, organization,
    /// priority, reportedDateTime, createdDateTime (default: id).
    /// </summary>
    public class FailureReportListRequestDto : PagedRequest
    {
        /// <summary>Looks in the reference number, the description and the equipment's Yaas code.</summary>
        public string? Search { get; set; }
        public string? ReferenceNumber { get; set; }
        [FromQuery(Name = "equipmentId")] public long[]? EquipmentIds { get; set; }
        [FromQuery(Name = "failureTypeId")] public long[]? FailureTypeIds { get; set; }
        [FromQuery(Name = "failureCategoryId")] public long[]? FailureCategoryIds { get; set; }
        [FromQuery(Name = "reportTypeId")] public long[]? ReportTypeIds { get; set; }
        [FromQuery(Name = "statusId")] public long[]? StatusIds { get; set; }
        [FromQuery(Name = "priorityId")] public int[]? PriorityIds { get; set; }
        [FromQuery(Name = "organizationId")] public long[]? OrganizationIds { get; set; }
        [FromQuery(Name = "employeeId")] public long[]? EmployeeIds { get; set; }
        [FromQuery(Name = "reportedByUserId")] public long[]? ReportedByUserIds { get; set; }
        /// <summary>Reported at or after (UTC, ISO 8601).</summary>
        public DateTime? ReportedFrom { get; set; }
        /// <summary>Reported at or before (UTC, ISO 8601), inclusive as sent.</summary>
        public DateTime? ReportedTo { get; set; }
        /// <summary>null = active reports only (default); true/false = only active / only deactivated.</summary>
        public bool? IsActive { get; set; }
    }
}
