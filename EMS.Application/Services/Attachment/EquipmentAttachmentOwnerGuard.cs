using EMS.Application.Common.DataAccess;
using EMS.Application.Interfaces.Attachment;
using EMS.Domain.Interfaces;

namespace EMS.Application.Services.Attachment
{
    /// <summary>The files of an equipment are visible to whoever may see the equipment (the equipment row policy).</summary>
    public sealed class EquipmentAttachmentOwnerGuard : IAttachmentOwnerGuard
    {
        private readonly IEquipmentRepository _equipment;
        private readonly IRowAccess _access;

        public EquipmentAttachmentOwnerGuard(IEquipmentRepository equipment, IRowAccess access)
        {
            _equipment = equipment;
            _access = access;
        }

        public string TableName => "Equipments";

        public Task<bool> IsVisibleAsync(long entityId, CancellationToken cancellationToken = default) =>
            _access.IsVisibleAsync(_equipment.QueryRowsIncludingInactive(), r => r.Id == entityId, cancellationToken);
    }
}
