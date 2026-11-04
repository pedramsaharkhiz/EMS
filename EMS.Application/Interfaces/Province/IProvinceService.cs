using EMS.Application.Dtos.Province;

namespace EMS.Application.Interfaces.Province
{
    public interface IProvinceService
        : ICatalogService<ProvinceCreateDto, ProvinceUpdateDto, ProvinceDetailDto, ProvinceListItemDto, ProvinceListRequestDto>
    {
    }
}
