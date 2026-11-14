using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeCreateDto : ICatalogCreateRequest
    {
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        /// <summary>The equipment types this failure type applies to. Empty or missing = it applies to every equipment type.</summary>
        public List<long>? EquipmentTypeIds { get; set; }
    }
}
