using EMS.Domain.Enums;

namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// Creates an equipment. Exactly one of EquipmentTypeId / EquipmentModelId is required.
    /// </summary>
    public class EquipmentCreateDto
    {
        public EquipmentOwner Owner { get; set; }
        public bool HasConflict { get; set; } = false;
        public string YaasCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? AssetNumber { get; set; }
        public long? EquipmentTypeId { get; set; }
        public long? EquipmentModelId { get; set; }
        public long EquipmentStatusId { get; set; }

        // The initial location (written to the location history as well): with a branch OR with a supporting department, exactly one of the two.
        public long? CurrentOrganizationId { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }

        public string? Description { get; set; }
    }
}
