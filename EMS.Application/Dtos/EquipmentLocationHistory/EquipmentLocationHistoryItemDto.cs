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
        // the city and province of that department (the list shows them for the current one)
        public long? CityId { get; set; }
        public string? CityTitle { get; set; }
        public long? ProvinceId { get; set; }
        public string? ProvinceTitle { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }
        // where the technician works NOW (the current department of that user; the entry does not record it)
        public long? TechnicianSupportingDepartmentId { get; set; }
        public string? TechnicianSupportingDepartmentTitle { get; set; }
        public long? TechnicianCityId { get; set; }
        public string? TechnicianCityTitle { get; set; }
        public long? TechnicianProvinceId { get; set; }
        public string? TechnicianProvinceTitle { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public long? ChangedByUserId { get; set; }
        public string? ChangedByName { get; set; }
        public string? Description { get; set; }
    }
}
