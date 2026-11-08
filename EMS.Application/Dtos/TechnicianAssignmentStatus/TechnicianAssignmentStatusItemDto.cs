namespace EMS.Application.Dtos.TechnicianAssignmentStatus
{
    public class TechnicianAssignmentStatusItemDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        /// <summary>Open = still being handled (assigned, accepted or in progress); false = final.</summary>
        public bool IsOpen { get; set; }
    }
}
