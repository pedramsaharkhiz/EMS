using EMS.Domain.Enums;
namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>One row of the failure report list. The ids are what edit forms need; the titles are what people read.</summary>
    public class FailureReportListItemDto
    {
        public long Id { get; set; }
        public string? ReferenceNumber { get; set; }
        public long EquipmentId { get; set; }
        public string? EquipmentYaasCode { get; set; }
        // the equipment, with the same names as in the equipment list
        public string? EquipmentSerialNumber { get; set; }
        public string? EquipmentAssetNumber { get; set; }
        public EquipmentOwner EquipmentOwner { get; set; }
        public string? EquipmentOwnerTitle { get; set; }
        /// <summary>The effective type: the equipment's own, or its model's.</summary>
        public long? EquipmentTypeId { get; set; }
        public string? EquipmentTypeTitle { get; set; }
        public long? EquipmentModelId { get; set; }
        public string? EquipmentModelTitle { get; set; }
        public long EquipmentStatusId { get; set; }
        public string? EquipmentStatusTitle { get; set; }
        public string? EquipmentStatusCode { get; set; }
        public long FailureTypeId { get; set; }
        public string? FailureTypeTitle { get; set; }
        public string? FailureTypeCode { get; set; }
        public long FailureCategoryId { get; set; }
        public string? FailureCategoryTitle { get; set; }
        public long ReportTypeId { get; set; }
        public string? ReportTypeTitle { get; set; }
        public string? ReportTypeCode { get; set; }
        public long FailureReportStatusId { get; set; }
        public string? FailureReportStatusTitle { get; set; }
        public string? FailureReportStatusCode { get; set; }
        public long OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }
        /// <summary>The supporting department that serves the report's branch (null when the branch has none yet).</summary>
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeePersonnelCode { get; set; }
        public long ReportedByUserId { get; set; }
        // the user who registered the report, with the same person set as a technician
        public string? ReportedByName { get; set; }
        public string? ReportedByPersonnelCode { get; set; }
        public bool? ReportedByIsActive { get; set; }
        public long? ReportedByPostId { get; set; }
        public string? ReportedByPostTitle { get; set; }
        public long? ReportedBySupportingDepartmentId { get; set; }
        public string? ReportedBySupportingDepartmentTitle { get; set; }
        public long? ReportedByCityId { get; set; }
        public string? ReportedByCityTitle { get; set; }
        public long? ReportedByProvinceId { get; set; }
        public string? ReportedByProvinceTitle { get; set; }
        /// <summary>1 (کم) to 5 (فوری).</summary>
        public int PriorityId { get; set; }
        public string PriorityTitle { get; set; } = string.Empty;
        public DateTime ReportedDateTime { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
