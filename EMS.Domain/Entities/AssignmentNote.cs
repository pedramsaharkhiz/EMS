namespace EMS.Domain.Entities;

public sealed class AssignmentNote : BaseEntity, EMS.Domain.Interfaces.IAuditableEntity
{
    public long AssignmentNoteId { get; set; }
    public long TechnicianAssignmentId { get; set; }
    public long NoteCategoryId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime? SeenDateTime { get; set; }
    public TechnicianAssignment TechnicianAssignment { get; set; } = null!;
    public NoteCategory NoteCategory { get; set; } = null!;
}
