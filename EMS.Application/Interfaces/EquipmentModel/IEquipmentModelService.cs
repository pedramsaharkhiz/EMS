using EMS.Application.Dtos.EquipmentModel;

namespace EMS.Application.Interfaces.EquipmentModel
{
    /// <summary>CRUD contract of the catalog "مدل تجهیز" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IEquipmentModelService
        : ICatalogService<EquipmentModelCreateDto, EquipmentModelUpdateDto, EquipmentModelDetailDto, EquipmentModelListItemDto, EquipmentModelListRequestDto>
    {
    }
}
