using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.City
{
    public class CityCreateDto : ICatalogCreateRequest
    {
        public long ProvinceId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}
