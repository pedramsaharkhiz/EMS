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
        /// <summary>
        /// Does the CALLER hold an assignment (any status) on a failure report of this equipment? Rows are per caller: the row-security policy uses it so a technician
        /// keeps seeing the equipment of the work he was given, whatever department it is in.
        /// </summary>
        public bool HasAssignmentOfCurrentUser { get; set; }
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

        /// <summary>Who the equipment is with (derived: the branch or the supporting department column that is filled).</summary>
        public EquipmentLocationKind LocationKind { get; set; }
        public string? LocationKindTitle { get; set; }
        public long? OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }
        public long? FieldId { get; set; }
        public string? FieldTitle { get; set; }
        public long? OrganizationManagementId { get; set; }
        public string? OrganizationManagementTitle { get; set; }
        public long? ZoneId { get; set; }
        public string? ZoneTitle { get; set; }
        /// <summary>The supporting department that serves the branch (not the department the equipment is at).</summary>
        public long? OrganizationSupportingDepartmentId { get; set; }
        public string? OrganizationSupportingDepartmentTitle { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? CityId { get; set; }
        public string? CityTitle { get; set; }
        public long? ProvinceId { get; set; }
        public string? ProvinceTitle { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }
        public string? TechnicianPersonnelCode { get; set; }
        public bool? TechnicianIsActive { get; set; }
        public long? TechnicianPostId { get; set; }
        public string? TechnicianPostTitle { get; set; }
        /// <summary>Where the technician works (his own department, city and province), not where the equipment is.</summary>
        /// <summary>The titles of the technician's departments, cities and provinces, each as one text separated by "، " (for the Excel file; the list gets the structured form from the person lookup).</summary>
        public string? TechnicianSupportingDepartmentTitle { get; set; }
        public string? TechnicianCityTitle { get; set; }
        public string? TechnicianProvinceTitle { get; set; }

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
        /// <summary>Who the equipment is with (derived: the branch or the supporting department column that is filled).</summary>
        public EquipmentLocationKind LocationKind { get; set; }
        public string? LocationKindTitle { get; set; }
        public long? OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? CityId { get; set; }
        public string? CityTitle { get; set; }
        public long? ProvinceId { get; set; }
        public string? ProvinceTitle { get; set; }
        /// <summary>The technician and the one who made the change: only the ids here, the people themselves come from one batched lookup.</summary>
        public long? TechnicianUserId { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public long? ChangedByUserId { get; set; }
        public string? Description { get; set; }
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
    }

    /// <summary>A user as a technician choice / assignment target, with only what the visibility rule reads (his supporting department).</summary>
    public sealed class UserDirectoryRow
    {
        public long Id { get; set; }
        /// <summary>The departments the user belongs to (the row policy asks whether the caller shares one).</summary>
        public IEnumerable<long> SupportingDepartmentIds { get; set; } = Array.Empty<long>();
    }

    public sealed class TechnicianInfo
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PersonnelCode { get; set; } = string.Empty;
        /// <summary>The departments the technician belongs to (what the row policy reads; the screens get the titles from the person lookup).</summary>
        public IEnumerable<long> SupportingDepartmentIds { get; set; } = Array.Empty<long>();
    }
}
