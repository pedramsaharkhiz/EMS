namespace EMS.Domain.Entities
{
    /// <summary>A system row per <see cref="Enums.TechnicianAssignmentStatusCode"/> (id == value, code == name); read-only through the API.</summary>
    public class TechnicianAssignmentStatus : BaseEntity, Interfaces.IAuditableEntity
    {
        public long TechnicianAssignmentStatusId { get; set; }
        public string Title { get; set; } = null!;
        public string Code { get; set; } = null!;
        public ICollection<TechnicianAssignment> TechnicianAssignments { get; set; } = new List<TechnicianAssignment>();
    }
}
