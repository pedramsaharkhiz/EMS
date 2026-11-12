namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>
    /// Assigns a failure report to a technician (<c>POST /api/FailureReport/{id}/assignments</c>). The technician may be any existing user.
    /// The <see cref="Title"/> and the <see cref="PriorityId"/> (1 کم to 5 فوری) are required; the deadline is optional and must be in the future.
    /// </summary>
    public class TechnicianAssignmentCreateDto
    {
        public long TechnicianUserId { get; set; }
        public string? Title { get; set; }
        public int PriorityId { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public string? Description { get; set; }
    }
}

namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>One technician of a bulk assignment. The <see cref="Title"/> (required) says what THIS technician must do (for example "تعمیر برق دستگاه"); the priority is his own.</summary>
    public class TechnicianAssignmentBulkItemDto
    {
        public long TechnicianUserId { get; set; }
        public string? Title { get; set; }
        public int PriorityId { get; set; }
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

namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>
    /// Edits an OPEN assignment (<c>PUT /api/TechnicianAssignment/{id}</c>): it REPLACES title, priority, description and deadline, like every other edit of the project
    /// (<see cref="Title"/> and <see cref="PriorityId"/> are required; a <see cref="Description"/> or <see cref="DeadlineDateTime"/> that is not sent is cleared).
    /// The technician and the status are not edited here (cancel and assign again; use the transitions). A deadline must be in the future when it CHANGES.
    /// </summary>
    public class TechnicianAssignmentUpdateDto
    {
        public string? Title { get; set; }
        public int PriorityId { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public string? Description { get; set; }
        /// <summary>Optional: send back the value read; a stale one fails with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
