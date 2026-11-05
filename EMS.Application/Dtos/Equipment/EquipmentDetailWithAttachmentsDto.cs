using EMS.Application.Dtos.Attachment;

namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// <see cref="EquipmentDetailDto"/> plus the equipment's active attachments, newest first (at most 20). Answered by GET /api/Equipment/{id}
    /// (and by the registration with files). Only the metadata is here: a file is downloaded from <c>GET /api/Attachments/{id}/download</c>.
    /// The other answers that carry an <see cref="EquipmentDetailDto"/> (update, move) do not list the attachments, only count them.
    /// </summary>
    public class EquipmentDetailWithAttachmentsDto : EquipmentDetailDto
    {
        public IReadOnlyList<AttachmentItemDto> Attachments { get; set; } = Array.Empty<AttachmentItemDto>();
    }
}
