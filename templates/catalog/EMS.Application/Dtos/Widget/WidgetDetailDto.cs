namespace EMS.Application.Dtos.Widget
{
    public class WidgetDetailDto
    {
        public long WidgetId { get; set; }
        public string Title { get; set; } = string.Empty;
//#if (HasCode)
        public string Code { get; set; } = string.Empty;
//#endif
        public bool IsActive { get; set; }

        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
