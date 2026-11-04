namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>
    /// Replaces the editable fields of a failure report: whatever is not sent is cleared (employee, reference number, description),
    /// except <see cref="ReportedDateTime"/>, which stays as it is when not sent. The registering user cannot be changed.
    /// </summary>
    public class FailureReportUpdateDto
    {
        public long EquipmentId { get; set; }
        public long FailureTypeId { get; set; }
        public long ReportTypeId { get; set; }
        public long FailureReportStatusId { get; set; }
        public long OrganizationId { get; set; }
        public long? EmployeeId { get; set; }
        public string? ReferenceNumber { get; set; }
        public int PriorityId { get; set; }
        public DateTime? ReportedDateTime { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
