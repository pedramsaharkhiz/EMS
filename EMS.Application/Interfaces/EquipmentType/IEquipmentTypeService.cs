using EMS.Application.Dtos.EquipmentType;

namespace EMS.Application.Interfaces.EquipmentType
{
    /// <summary>CRUD contract of the catalog "نوع تجهیز" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IEquipmentTypeService
        : ICatalogService<EquipmentTypeCreateDto, EquipmentTypeUpdateDto, EquipmentTypeDetailDto, EquipmentTypeListItemDto, EquipmentTypeListRequestDto>
    {
        /// <summary>The active types that have no active model, with their category (paged, filterable, sortable).</summary>
        Task<EMS.Application.Common.Models.PagedResult<EquipmentTypeWithCategoryDto>> GetWithoutModelsAsync(EquipmentTypeWithoutModelsRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>The type (and its category) of a model, active or not (404 when the model does not exist).</summary>
        Task<EquipmentTypeWithCategoryDto> GetByModelIdAsync(long modelId, CancellationToken cancellationToken = default);
    }
}
