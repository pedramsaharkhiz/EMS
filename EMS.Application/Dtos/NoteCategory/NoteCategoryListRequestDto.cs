using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.NoteCategory
{
    public class NoteCategoryListRequestDto : PagedRequest, ICatalogListRequest
    {
        public string? Title { get; set; }

        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}
