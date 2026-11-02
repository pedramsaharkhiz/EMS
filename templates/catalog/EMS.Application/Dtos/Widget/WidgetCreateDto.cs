using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.Widget
{
    public class WidgetCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
//#if (HasCode)
        public string Code { get; set; } = string.Empty;
//#endif
    }
}
