using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentListRequestDto : PagedRequest, ICatalogListRequest
    {
        /// <summary>Only the departments of this city.</summary>
        public long? CityId { get; set; }

        /// <summary>Only the departments of the cities of this province.</summary>
        public long? ProvinceId { get; set; }
        public string? Title { get; set; }

        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}
