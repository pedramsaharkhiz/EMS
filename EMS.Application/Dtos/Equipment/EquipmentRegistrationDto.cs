namespace EMS.Application.Dtos.Equipment
{
    /// <summary>
    /// The result of registering an equipment together with its files: the same as GET /api/Equipment/{id} (so a client can use one type),
    /// with the attachments that were stored (empty when no file was sent).
    /// </summary>
    public class EquipmentRegistrationDto : EquipmentDetailWithAttachmentsDto
    {
    }
}
