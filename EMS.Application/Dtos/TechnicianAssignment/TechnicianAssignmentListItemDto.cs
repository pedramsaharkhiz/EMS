using EMS.Application.Dtos.Common;
namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>
    /// One technician assignment. <c>statusCode</c> is Assigned, Accepted, Rejected, InProgress, Completed or Cancelled.
    /// <c>isOverdue</c> = it has a deadline in the past and is still open (assigned, accepted or in progress).
    /// </summary>
    public class TechnicianAssignmentListItemDto
    {
        public long Id { get; set; }
        public long FailureReportId { get; set; }
        public string? FailureReportReferenceNumber { get; set; }
        public long EquipmentId { get; set; }
        public string? EquipmentYaasCode { get; set; }

        public long StatusId { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusTitle { get; set; }

        public long TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }
        public string? TechnicianPersonnelCode { get; set; }
        public bool? TechnicianIsActive { get; set; }
        public long? TechnicianPostId { get; set; }
        public string? TechnicianPostTitle { get; set; }
        /// <summary>Where he works: every supporting department he belongs to (empty when none).</summary>
        public List<PersonDepartmentDto> TechnicianSupportingDepartments { get; set; } = [];

        /// <summary>A short title; <see cref="Description"/> is the full explanation.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>1 (کم) to 5 (فوری).</summary>
        public int PriorityId { get; set; }
        public string PriorityTitle { get; set; } = string.Empty;
        /// <summary>When the technician first saw the assignment; null = not seen yet.</summary>
        public DateTime? ViewedDateTime { get; set; }
        public bool IsSeen { get; set; }

        public long AssignedByUserId { get; set; }
        public string? AssignedByName { get; set; }
        public string? AssignedByPersonnelCode { get; set; }
        public bool? AssignedByIsActive { get; set; }
        public long? AssignedByPostId { get; set; }
        public string? AssignedByPostTitle { get; set; }
        /// <summary>Where he works: every supporting department he belongs to (empty when none).</summary>
        public List<PersonDepartmentDto> AssignedBySupportingDepartments { get; set; } = [];

        public DateTime AssignedDateTime { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public bool IsOverdue { get; set; }
        /// <summary>The assigner's note.</summary>
        public string? Description { get; set; }
        /// <summary>The reason of a rejection or cancellation, or the technician's note on completion.</summary>
        public string? ResultNote { get; set; }
        public int ActionCount { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
