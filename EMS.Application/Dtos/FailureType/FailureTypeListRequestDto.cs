using EMS.Application.Common.Catalog;
using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeListRequestDto : PagedRequest, ICatalogListRequest
    {
        public string? Title { get; set; }
        public string? Code { get; set; }
        public long? FailureCategoryId { get; set; }
        /// <summary>
        /// The failure types that apply to this equipment type: the ones linked to it AND the ones linked to no equipment type (those apply to all).
        /// This is the dropdown of a failure report: send the equipment's effective type id.
        /// </summary>
        public long? EquipmentTypeId { get; set; }

        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}
