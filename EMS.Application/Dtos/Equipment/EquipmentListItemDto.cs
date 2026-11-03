namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// One row of the equipment list, and one row of the Excel export (so they always show the same columns). The ids are what
    /// edit forms need; the titles are what people read. "Effective type" is the type of the equipment itself or, when it
    /// has a model instead, the type of that model.
    /// </summary>
    public class EquipmentListItemDto
    {
        public long Id { get; set; }
        public string YaasCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? AssetNumber { get; set; }

        public long? EquipmentTypeId { get; set; }
        public long? EquipmentModelId { get; set; }
        public string? EquipmentModelTitle { get; set; }
        public long? BrandId { get; set; }
        public string? BrandTitle { get; set; }
        public long? EffectiveTypeId { get; set; }
        public string? EffectiveTypeTitle { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryTitle { get; set; }

        public long EquipmentStatusId { get; set; }
        public string? EquipmentStatusTitle { get; set; }

        public long CurrentEquipmentLocationTypeId { get; set; }
        public string? CurrentEquipmentLocationTypeTitle { get; set; }
        public long? CurrentOrganizationId { get; set; }
        public string? CurrentOrganizationTitle { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public string? CurrentSupportingDepartmentTitle { get; set; }
        public long? CityId { get; set; }
        public string? CityTitle { get; set; }
        public long? ProvinceId { get; set; }
        public string? ProvinceTitle { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }

        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
