using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class Widget : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long WidgetId { get; set; }
        public string Title { get; set; } = null!;
//#if (HasCode)
        public string? Code { get; set; }
//#endif
    }
}
