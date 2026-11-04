using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    [Attachable("تجهیز", "Equipments")]
    public class Equipment : BaseEntity, IAuditableEntity
    {
        public long EquipmentId { get; set; }
        public string? SerialNumber { get; set; }
        public string? AssetNumber { get; set; }
        public string YaasCode { get; set; } = null!;
        public long? EquipmentTypeId { get; set; }
        public long? EquipmentModelId { get; set; }
        public long EquipmentStatusId { get; set; }
        public long CurrentEquipmentLocationTypeId { get; set; }
        public long? CurrentOrganizationId { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? Description { get; set; }

        public EquipmentModel? EquipmentModel { get; set; }
        public EquipmentType? EquipmentType { get; set; }
        public EquipmentStatus EquipmentStatus { get; set; } = null!;
        public EquipmentLocationType CurrentEquipmentLocationType { get; set; } = null!;
        public Organization? CurrentOrganization { get; set; }
        public SupportingDepartment? CurrentSupportingDepartment { get; set; }

        public ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();
        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
    }
}