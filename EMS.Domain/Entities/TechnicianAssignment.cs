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
        public DateTime AssignedDateTime { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public string? Description { get; set; }

        public FailureReport FailureReport { get; set; } = null!;
        public TechnicianAssignmentStatus TechnicianAssignmentStatus { get; set; } = null!;
        public ICollection<TechnicianAssignmentAction> TechnicianAssignmentActions { get; set; } = new List<TechnicianAssignmentAction>();
    }
}