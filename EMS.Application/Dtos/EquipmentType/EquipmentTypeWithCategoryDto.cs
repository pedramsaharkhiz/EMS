using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.EquipmentType
{
    /// <summary>
    /// The types that have no model. <c>sortBy</c>: title, code, category (default: title). Text filter: <c>title</c> contains. Only active types
    /// that have no active model are listed.
    /// </summary>
    public class EquipmentTypeWithoutModelsRequestDto : PagedRequest
    {
        public string? Title { get; set; }
        public long? EquipmentCategoryId { get; set; }
    }
}
