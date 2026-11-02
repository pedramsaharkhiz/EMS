namespace EMS.Application.Dtos.Widget
{
    public class WidgetListItemDto
    {
        public long WidgetId { get; set; }
        public string Title { get; set; } = string.Empty;
//#if (HasCode)
        public string Code { get; set; } = string.Empty;
//#endif
        public bool IsActive { get; set; }
    }
}
