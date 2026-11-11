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

namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>One technician of a bulk assignment. <see cref="Description"/> is required: it says what THIS technician must do (for example "repair the electrics").</summary>
    public class TechnicianAssignmentBulkItemDto
    {
        public long TechnicianUserId { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public string? Description { get; set; }
    }

    /// <summary>
    /// Assigns one failure report to several technicians at once (<c>POST /api/FailureReport/{id}/assignments/bulk</c>), each with his own task, deadline and
    /// later his own accept / start / complete. All or nothing: if any item is refused, nothing is saved and the error lists every problem per item.
    /// </summary>
    public class TechnicianAssignmentBulkCreateDto
    {
        public List<TechnicianAssignmentBulkItemDto> Items { get; set; } = new();
    }
}
