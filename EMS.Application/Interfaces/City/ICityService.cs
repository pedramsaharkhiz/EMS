using EMS.Application.Dtos.City;

namespace EMS.Application.Interfaces.City
{
    public interface ICityService
        : ICatalogService<CityCreateDto, CityUpdateDto, CityDetailDto, CityListItemDto, CityListRequestDto>
    {
    }
}
