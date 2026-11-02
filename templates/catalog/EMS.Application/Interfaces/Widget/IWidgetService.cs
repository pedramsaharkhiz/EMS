using EMS.Application.Dtos.Widget;

namespace EMS.Application.Interfaces.Widget
{
    public interface IWidgetService
        : ICatalogService<WidgetCreateDto, WidgetUpdateDto, WidgetDetailDto, WidgetListItemDto, WidgetListRequestDto>
    {
    }
}
