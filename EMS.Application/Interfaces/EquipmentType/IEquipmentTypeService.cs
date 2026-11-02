using EMS.Application.Dtos.EquipmentType;

namespace EMS.Application.Interfaces.EquipmentType
{
    /// <summary>CRUD contract of the catalog "نوع تجهیز" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IEquipmentTypeService
        : ICatalogService<EquipmentTypeCreateDto, EquipmentTypeUpdateDto, EquipmentTypeDetailDto, EquipmentTypeListItemDto, EquipmentTypeListRequestDto>
    {
    }
}
