using EMS.Application.Dtos.FailureCategory;

namespace EMS.Application.Interfaces.FailureCategory
{
    /// <summary>CRUD contract of the catalog "دسته‌بندی خرابی" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IFailureCategoryService
        : ICatalogService<FailureCategoryCreateDto, FailureCategoryUpdateDto, FailureCategoryDetailDto, FailureCategoryListItemDto, FailureCategoryListRequestDto>
    {
    }
}
