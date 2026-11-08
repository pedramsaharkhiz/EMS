namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>Records an action the technician performed (only while the assignment is in progress). <see cref="ActionDateTime"/> defaults to now.</summary>
    public class TechnicianAssignmentActionCreateDto
    {
        public long ActionId { get; set; }
        public DateTime? ActionDateTime { get; set; }
        public long? EquipmentLocationHistoryId { get; set; }
        public string? Description { get; set; }
    }
}
