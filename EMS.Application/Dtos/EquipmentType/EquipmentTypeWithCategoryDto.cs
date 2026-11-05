using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.EquipmentType
{
    /// <summary>An equipment type with its category: what the equipment form needs to fill the type (and its category) in.</summary>
    public class EquipmentTypeWithCategoryDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        public long EquipmentCategoryId { get; set; }
        public string? EquipmentCategoryTitle { get; set; }
    }

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
