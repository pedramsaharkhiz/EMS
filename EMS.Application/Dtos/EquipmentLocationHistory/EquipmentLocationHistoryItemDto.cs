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
        // the technician, as everywhere (his own place of work is the CURRENT one: the entry does not record it)
        public string? TechnicianName { get; set; }
        public string? TechnicianPersonnelCode { get; set; }
        public bool? TechnicianIsActive { get; set; }
        public long? TechnicianPostId { get; set; }
        public string? TechnicianPostTitle { get; set; }
        public long? TechnicianSupportingDepartmentId { get; set; }
        public string? TechnicianSupportingDepartmentTitle { get; set; }
        public long? TechnicianCityId { get; set; }
        public string? TechnicianCityTitle { get; set; }
        public long? TechnicianProvinceId { get; set; }
        public string? TechnicianProvinceTitle { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public long? ChangedByUserId { get; set; }
        // who made the change, with the same person set as the technician
        public string? ChangedByName { get; set; }
        public string? ChangedByPersonnelCode { get; set; }
        public bool? ChangedByIsActive { get; set; }
        public long? ChangedByPostId { get; set; }
        public string? ChangedByPostTitle { get; set; }
        public long? ChangedBySupportingDepartmentId { get; set; }
        public string? ChangedBySupportingDepartmentTitle { get; set; }
        public long? ChangedByCityId { get; set; }
        public string? ChangedByCityTitle { get; set; }
        public long? ChangedByProvinceId { get; set; }
        public string? ChangedByProvinceTitle { get; set; }
        public string? Description { get; set; }
    }
}
