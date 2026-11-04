namespace EMS.Application.Dtos.FailureReport
{
    /// <summary>
    /// Registers a failure report. It starts as "باز" (Open) and is attributed to the signed-in user. The branch (<see cref="OrganizationId"/>)
    /// is always given by the client. <see cref="PriorityId"/> is 1 (کم) to 5 (فوری). <see cref="ReportedDateTime"/> defaults to now
    /// and may not be in the future.
    /// </summary>
    public class FailureReportCreateDto
    {
        public long EquipmentId { get; set; }
        public long FailureTypeId { get; set; }
        public long ReportTypeId { get; set; }
        public long OrganizationId { get; set; }
        public long? EmployeeId { get; set; }
        public string? ReferenceNumber { get; set; }
        public int PriorityId { get; set; }
        public DateTime? ReportedDateTime { get; set; }
        public string? Description { get; set; }
    }
}
