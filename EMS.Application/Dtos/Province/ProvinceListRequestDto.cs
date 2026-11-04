using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Province
{
    public class ProvinceListRequestDto : PagedRequest, ICatalogListRequest
    {
        public string? Title { get; set; }
        public string? Code { get; set; }

        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}
