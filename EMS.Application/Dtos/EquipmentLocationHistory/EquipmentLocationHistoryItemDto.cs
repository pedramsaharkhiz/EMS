using EMS.Domain.Enums;

namespace EMS.Application.Dtos.EquipmentLocationHistory
{
    public class EquipmentLocationHistoryItemDto
    {
        public long Id { get; set; }
        /// <summary>Who the equipment was with in this entry: 1 = a branch, 2 = a supporting department.</summary>
        public EquipmentLocationKind LocationKind { get; set; }
        public string? LocationKindTitle { get; set; }
        public long? CurrentOrganizationId { get; set; }
        public string? CurrentOrganizationTitle { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public string? CurrentSupportingDepartmentTitle { get; set; }
        // the city and province of that department (the location AT THE TIME of this entry; the names match the list's current* fields)
        public long? CurrentCityId { get; set; }
        public string? CurrentCityTitle { get; set; }
        public long? CurrentProvinceId { get; set; }
        public string? CurrentProvinceTitle { get; set; }
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
