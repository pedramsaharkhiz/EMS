using EMS.Domain.Enums;

namespace EMS.Domain.ReadModels
{
    /// <summary>
    /// One equipment flattened with the titles of everything it refers to. Lists, the detail and the Excel export all read this
    /// shape, so filters, sorting and columns are written once. "Effective" type means the type the equipment has itself or
    /// (when it has a model instead) the type of its model; the category is the category of that type.
    /// </summary>
    public sealed class EquipmentRow
    {
        public long Id { get; set; }
        public EquipmentOwner Owner { get; set; }
        /// <summary>The Persian title of <see cref="Owner"/> (the Excel export reads it; a column must read a property of the row).</summary>
        public string OwnerTitle { get; set; } = string.Empty;
        public string YaasCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }
        public string? AssetNumber { get; set; }

        /// <summary>The type as stored (null when the equipment has a model).</summary>
        public long? EquipmentTypeId { get; set; }
        /// <summary>The model as stored (null when the equipment has a type).</summary>
        public long? EquipmentModelId { get; set; }
        public string? EquipmentModelTitle { get; set; }
        public long? BrandId { get; set; }
        public string? BrandTitle { get; set; }
        public long? EffectiveTypeId { get; set; }
        public string? EffectiveTypeTitle { get; set; }
        public long? CategoryId { get; set; }
        public string? CategoryTitle { get; set; }

        public long StatusId { get; set; }
        public string? StatusTitle { get; set; }
        public string? StatusCode { get; set; }

        public long LocationTypeId { get; set; }
        public string? LocationTypeTitle { get; set; }
        public long? OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }
        public long? FieldId { get; set; }
        public long? OrganizationManagementId { get; set; }
        public long? ZoneId { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? CityId { get; set; }
        public string? CityTitle { get; set; }
        public long? ProvinceId { get; set; }
        public string? ProvinceTitle { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }

        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    /// <summary>One entry of an equipment's location history, with titles and names.</summary>
    public sealed class EquipmentLocationHistoryRow
    {
        public long Id { get; set; }
        public long EquipmentId { get; set; }
        public long LocationTypeId { get; set; }
        public string? LocationTypeTitle { get; set; }
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

    /// <summary>A user who can be chosen as the technician of an equipment.</summary>
    /// <summary>Whether a user can be the technician of an equipment: active AND holding the post "Technician" (<c>PostCode.Technician</c>).</summary>
    public enum TechnicianEligibility
    {
        NotFound = 0,
        Inactive = 1,
        /// <summary>The user exists and is active but does not hold the post "Technician".</summary>
        NotTechnician = 2,
        Eligible = 3,
    }

    /// <summary>The technician of one equipment with what the screens show next to the name (active or not: a past assignment keeps its person).</summary>
    public sealed class EquipmentTechnicianRow
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public long? PostId { get; set; }
        public string? PostTitle { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
    }

    public sealed class TechnicianInfo
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
    }
}
