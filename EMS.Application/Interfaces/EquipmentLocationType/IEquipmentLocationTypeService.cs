using EMS.Application.Dtos.EquipmentLocationType;

namespace EMS.Application.Interfaces.EquipmentLocationType
{
    /// <summary>CRUD contract of the catalog "نوع محل تجهیز" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IEquipmentLocationTypeService
        : ICatalogService<EquipmentLocationTypeCreateDto, EquipmentLocationTypeUpdateDto, EquipmentLocationTypeDetailDto, EquipmentLocationTypeListItemDto, EquipmentLocationTypeListRequestDto>
    {
    }
}
