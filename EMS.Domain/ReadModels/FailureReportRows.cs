namespace EMS.Domain.ReadModels
{
    /// <summary>One failure report flattened with the titles and names of everything it refers to (list and detail read this).</summary>
    public sealed class FailureReportRow
    {
        public long Id { get; set; }
        public string? ReferenceNumber { get; set; }

        public long EquipmentId { get; set; }
        public string? EquipmentYaasCode { get; set; }

        public long FailureTypeId { get; set; }
        public string? FailureTypeTitle { get; set; }
        public long FailureCategoryId { get; set; }
        public string? FailureCategoryTitle { get; set; }

        public long ReportTypeId { get; set; }
        public string? ReportTypeTitle { get; set; }

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

        public long ReportedByUserId { get; set; }
        public string? ReportedByName { get; set; }

        public int PriorityId { get; set; }
        public DateTime ReportedDateTime { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }
}
