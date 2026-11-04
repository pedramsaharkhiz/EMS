namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>One row of the failure report list. The ids are what edit forms need; the titles are what people read.</summary>
    public class FailureReportListItemDto
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
        public long FailureReportStatusId { get; set; }
        public string? FailureReportStatusTitle { get; set; }
        public string? FailureReportStatusCode { get; set; }
        public long OrganizationId { get; set; }
        public string? OrganizationTitle { get; set; }
        public long? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public long ReportedByUserId { get; set; }
        public string? ReportedByName { get; set; }
        /// <summary>1 (کم) to 5 (فوری).</summary>
        public int PriorityId { get; set; }
        public string PriorityTitle { get; set; } = string.Empty;
        public DateTime ReportedDateTime { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
    }
}
