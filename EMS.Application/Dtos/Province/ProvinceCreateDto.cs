using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.Province
{
    public class ProvinceCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
    }
}
