namespace EMS.Domain.ReadModels
{
    /// <summary>One technician assignment flattened with the report, equipment and status it refers to (list and detail read this).</summary>
    public sealed class TechnicianAssignmentRow
    {
        public long Id { get; set; }
        public long FailureReportId { get; set; }
        public string? FailureReportReferenceNumber { get; set; }
        public long EquipmentId { get; set; }
        public string? EquipmentYaasCode { get; set; }
        public long StatusId { get; set; }
        public string? StatusCode { get; set; }
        public string? StatusTitle { get; set; }
        public long TechnicianUserId { get; set; }
        public long AssignedByUserId { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public DateTime? DeadlineDateTime { get; set; }
        public DateTime? CompletedDateTime { get; set; }
        public string? Description { get; set; }
        public string? ResultNote { get; set; }
        public int ActionCount { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifiedDateTime { get; set; }
    }

    public sealed class TechnicianAssignmentActionRow
    {
        public long Id { get; set; }
        public long TechnicianAssignmentId { get; set; }
        public long ActionId { get; set; }
        public string? ActionTitle { get; set; }
        public string? ActionCode { get; set; }
        public long? EquipmentLocationHistoryId { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string? Description { get; set; }
        public long? CreatedByUserId { get; set; }
    }
}
