using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Attachment
{
    public class AttachmentItemDto
    {
        public long Id { get; set; }
        public string TableName { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public long? CreatedByUserId { get; set; }
        // who uploaded it, with the same person set as a technician
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

    public class AttachmentListRequestDto : PagedRequest
    {
        /// <summary>null/true = active attachments (default), false = the deactivated ones.</summary>
        public bool? IsActive { get; set; }
    }
}
