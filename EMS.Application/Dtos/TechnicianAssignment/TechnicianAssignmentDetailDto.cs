namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>The list item plus the actions recorded so far and the <see cref="RowVersion"/>.</summary>
    public class TechnicianAssignmentDetailDto : TechnicianAssignmentListItemDto
    {
        public List<TechnicianAssignmentActionItemDto> Actions { get; set; } = new();
        public DateTime? ModifiedDateTime { get; set; }
        /// <summary>Send this value back in a transition request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
