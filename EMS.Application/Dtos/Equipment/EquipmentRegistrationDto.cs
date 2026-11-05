using EMS.Application.Dtos.Attachment;

namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// The result of registering an equipment together with its files: everything <see cref="EquipmentDetailDto"/> has (so a client can
    /// use the same type as for a plain registration) plus the attachments that were stored (empty when no file was sent).
    /// </summary>
    public class EquipmentRegistrationDto : EquipmentDetailDto
    {
        public IReadOnlyList<AttachmentItemDto> Attachments { get; set; } = Array.Empty<AttachmentItemDto>();
    }
}
