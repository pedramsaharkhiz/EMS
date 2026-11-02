using EMS.Application.Dtos.EquipmentCategory;

namespace EMS.Application.Interfaces.EquipmentCategory
{
    /// <summary>CRUD contract of the catalog "دسته‌بندی" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IEquipmentCategoryService
        : ICatalogService<EquipmentCategoryCreateDto, EquipmentCategoryUpdateDto, EquipmentCategoryDetailDto, EquipmentCategoryListItemDto, EquipmentCategoryListRequestDto>
    {
    }
}
