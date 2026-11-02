using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Widget
{
    public class WidgetListRequestDto : PagedRequest, ICatalogListRequest
    {
        public string? Title { get; set; }
//#if (HasCode)
        public string? Code { get; set; }
//#endif

        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}
