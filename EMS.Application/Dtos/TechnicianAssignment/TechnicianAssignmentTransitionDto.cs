namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>
    /// Body of accept / reject / start / complete / cancel. <see cref="Note"/> is required for reject and cancel (the reason) and optional
    /// for the others. <see cref="RowVersion"/> is optional; when sent and stale the request fails with 409.
    /// </summary>
    public class TechnicianAssignmentTransitionDto
    {
        public string? Note { get; set; }
        public string? RowVersion { get; set; }
    }
}

namespace EMS.Application.Dtos.TechnicianAssignment
{
    /// <summary>Body of reject and cancel: the reason is required.</summary>
    public class TechnicianAssignmentReasonDto : TechnicianAssignmentTransitionDto { }
}
