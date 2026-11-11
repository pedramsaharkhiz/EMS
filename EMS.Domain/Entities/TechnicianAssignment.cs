using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class TechnicianAssignment : BaseEntity, IAuditableEntity
    {
        public long TechnicianAssignmentId { get; set; }
        public long FailureReportId { get; set; }
        public long TechnicianAssignmentStatusId { get; set; }
        public long TechnicianUserId { get; set; }
        public long AssignedByUserId { get; set; }
        /// <summary>A short title (what the list shows, e.g. "تعمیر برق"); <see cref="Description"/> is the full explanation.</summary>
        public string Title { get; set; } = null!;
        /// <summary>1 (کم) to 5 (فوری): the same scale as a failure report; independent of the report's own priority.</summary>
        public int PriorityId { get; set; }
        /// <summary>When the technician first saw the assignment (null = not yet). Set once, by the technician himself, and never changed.</summary>
        public DateTime? ViewedDateTime { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        /// <summary>The assigner's note.</summary>
        public string? Description { get; set; }
        /// <summary>The reason of a rejection or cancellation, or the technician's note on completion.</summary>
        public string? ResultNote { get; set; }

        public FailureReport FailureReport { get; set; } = null!;
        public TechnicianAssignmentStatus TechnicianAssignmentStatus { get; set; } = null!;
        public ICollection<TechnicianAssignmentAction> TechnicianAssignmentActions { get; set; } = new List<TechnicianAssignmentAction>();
    }
}