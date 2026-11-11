using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.AssignmentNote;

public class AssignmentNoteCreateDto
{
    public long NoteCategoryId { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class AssignmentNoteUpdateDto : AssignmentNoteCreateDto
{
    public string? RowVersion { get; set; }
}

public sealed class AssignmentNoteSeenDto
{
    public string? RowVersion { get; set; }
}

public sealed class AssignmentNoteListRequestDto : PagedRequest
{
    public long? NoteCategoryId { get; set; }
    public string? Search { get; set; }
    public bool? IsSeen { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AssignmentNoteItemDto
{
    public long Id { get; set; }
    public long TechnicianAssignmentId { get; set; }
    public long NoteCategoryId { get; set; }
    public string NoteCategoryTitle { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime? SeenDateTime { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public string RowVersion { get; set; } = "0";
        public long? CreatedByUserId { get; set; }
        public string? CreatedByName { get; set; }
        public string? CreatedByPersonnelCode { get; set; }
        public bool? CreatedByIsActive { get; set; }
        public long? CreatedByPostId { get; set; }
        public string? CreatedByPostTitle { get; set; }
        public long? CreatedBySupportingDepartmentId { get; set; }
        public string? CreatedBySupportingDepartmentTitle { get; set; }
        public long? CreatedByCityId { get; set; }
        public string? CreatedByCityTitle { get; set; }
        public long? CreatedByProvinceId { get; set; }
        public string? CreatedByProvinceTitle { get; set; }
}
