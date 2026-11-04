using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.ActionEntity
{
    public class ActionEntityCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
