using EMS.Application.Dtos.EquipmentStatus;

namespace EMS.Application.Interfaces.EquipmentStatus
{
    /// <summary>CRUD contract of the catalog "وضعیت تجهیز" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IEquipmentStatusService
        : ICatalogService<EquipmentStatusCreateDto, EquipmentStatusUpdateDto, EquipmentStatusDetailDto, EquipmentStatusListItemDto, EquipmentStatusListRequestDto>
    {
    }
}
