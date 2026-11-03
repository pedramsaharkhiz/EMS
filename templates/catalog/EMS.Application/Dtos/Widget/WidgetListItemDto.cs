namespace EMS.Application.Dtos.Widget
{
    public class WidgetListItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
//#if (HasCode)
        public string? Code { get; set; }
//#endif
        public bool IsActive { get; set; }
    }
}
