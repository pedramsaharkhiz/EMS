using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeCreateDto : ICatalogCreateRequest
    {
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
