using EMS.Application.Dtos.Common;
using EMS.Domain.Enums;

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
        /// <summary>How many active attachments (files) this equipment has. The list of them is in <see cref="EquipmentDetailWithAttachmentsDto.Attachments"/> (GET /api/Equipment/{id}).</summary>
        public int AttachmentCount { get; set; }
        public EquipmentOwner Owner { get; set; }
        public bool HasConflict { get; set; } = false;
        /// <summary>The Persian title of <see cref="Owner"/> (شرکت / بانک), so a client does not have to map the number.</summary>
        public string OwnerTitle { get; set; } = string.Empty;
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
        /// <summary>The stable code of the status (the name of <c>EquipmentStatusCode</c>, e.g. "InStock").</summary>
        public string? EquipmentStatusCode { get; set; }

        /// <summary>Who the equipment is with: 1 = a branch (<c>currentOrganization...</c>), 2 = a supporting department (<c>currentSupportingDepartment...</c>). Exactly one of the two is filled.</summary>
        public EquipmentLocationKind LocationKind { get; set; }
        public string? LocationKindTitle { get; set; }
        public long? CurrentOrganizationId { get; set; }
        public string? CurrentOrganizationTitle { get; set; }
        // where the branch sits in the hierarchy (the same levels the list can be filtered by)
        public long? FieldId { get; set; }
        public string? FieldTitle { get; set; }
        public long? OrganizationManagementId { get; set; }
        public string? OrganizationManagementTitle { get; set; }
        public long? ZoneId { get; set; }
        public string? ZoneTitle { get; set; }
        /// <summary>The supporting department that SERVES the branch (not the department the equipment is at: that is <c>currentSupportingDepartment...</c>).</summary>
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public string? CurrentSupportingDepartmentTitle { get; set; }
        public long? CurrentCityId { get; set; }
        public string? CurrentCityTitle { get; set; }
        public long? CurrentProvinceId { get; set; }
        public string? CurrentProvinceTitle { get; set; }
        public long? TechnicianUserId { get; set; }
        public string? TechnicianName { get; set; }
        public string? TechnicianPersonnelCode { get; set; }
        /// <summary>Is the technician an active user? (an equipment keeps its technician after he is deactivated). Null when there is none.</summary>
        public bool? TechnicianIsActive { get; set; }
        public long? TechnicianPostId { get; set; }
        public string? TechnicianPostTitle { get; set; }
        // where the TECHNICIAN works: his own supporting department and its city and province (not where the equipment is)
        /// <summary>Where he works: every supporting department he belongs to (empty when none).</summary>
        public List<PersonDepartmentDto> TechnicianSupportingDepartments { get; set; } = [];

        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
