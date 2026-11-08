namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>
    /// Assigns a failure report to a technician (<c>POST /api/FailureReport/{id}/assignments</c>). The technician may be any existing user.
    /// The deadline is optional and must be in the future.
    /// </summary>
    public class TechnicianAssignmentCreateDto
    {
        public long TechnicianUserId { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public string? Description { get; set; }
    }
}
