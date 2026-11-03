namespace EMS.Application.Dtos.EquipmentLocationHistory
{
    public class EquipmentLocationHistoryItemDto
    {
        public long Id { get; set; }
        public long EquipmentLocationTypeId { get; set; }
        public string? EquipmentLocationTypeTitle { get; set; }
        public long? OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public long? ChangedByUserId { get; set; }
        public string? ChangedByName { get; set; }
        public string? Description { get; set; }
    }
}
