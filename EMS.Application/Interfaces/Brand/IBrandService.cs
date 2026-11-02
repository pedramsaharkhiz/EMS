using EMS.Application.Dtos.Brand;

namespace EMS.Application.Interfaces.Brand
{
    /// <summary>CRUD contract of the catalog "برند" (see <see cref="ICatalogService{TCreate, TUpdate, TDetail, TListItem, TListRequest}"/>).</summary>
    public interface IBrandService
        : ICatalogService<BrandCreateDto, BrandUpdateDto, BrandDetailDto, BrandListItemDto, BrandListRequestDto>
    {
    }
}
