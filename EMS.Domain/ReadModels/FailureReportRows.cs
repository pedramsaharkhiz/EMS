using EMS.Domain.Enums;
namespace EMS.Domain.ReadModels
{
    /// <summary>One failure report flattened with the titles and names of everything it refers to (list and detail read this).</summary>
    public sealed class FailureReportRow
    {
        public long Id { get; set; }
        public string? ReferenceNumber { get; set; }

        public long EquipmentId { get; set; }
        public string? EquipmentYaasCode { get; set; }
        public string? EquipmentSerialNumber { get; set; }
        public string? EquipmentAssetNumber { get; set; }
        public EquipmentOwner EquipmentOwner { get; set; }
        public string? EquipmentOwnerTitle { get; set; }
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

        public long StatusId { get; set; }
        public string? StatusTitle { get; set; }
        public string? StatusCode { get; set; }

        public long OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }

        /// <summary>The supporting department that serves the report's branch (null when the branch has none yet).</summary>
        public long? SupportingDepartmentId { get; set; }
        public string? SupportingDepartmentTitle { get; set; }

        public long? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeePersonnelCode { get; set; }

        /// <summary>Does the report have an open assignment (assigned, accepted or in progress)?</summary>
        public bool HasOpenAssignment { get; set; }
        /// <summary>Does the CALLER hold an assignment (any status) on this report? Rows are per caller: the row-security policy lets a technician see the reports of his work.</summary>
        public bool HasAssignmentOfCurrentUser { get; set; }

        public long ReportedByUserId { get; set; }
        // the user who registered the report, with the same person set as every other screen (list, detail and Excel read these, not a second lookup)
        public string? ReportedByName { get; set; }
        public string? ReportedByPersonnelCode { get; set; }
        public bool? ReportedByIsActive { get; set; }
        public long? ReportedByPostId { get; set; }
        public string? ReportedByPostTitle { get; set; }
        /// <summary>The titles of the reporter's departments, cities and provinces, each as one text separated by "، " (for the Excel file; the list gets the structured form from the person lookup).</summary>
        public string? ReportedBySupportingDepartmentTitle { get; set; }
        public string? ReportedByCityTitle { get; set; }
        public string? ReportedByProvinceTitle { get; set; }

        public int PriorityId { get; set; }
        /// <summary>The Persian title of <see cref="PriorityId"/> (computed here, not stored).</summary>
        public string PriorityTitle => FailureReportPriorities.TitleOf(PriorityId);
        public DateTime ReportedDateTime { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
