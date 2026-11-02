using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.Widget
{
    public class WidgetUpdateDto : ICatalogUpdateRequest
    {
        public long WidgetId { get; set; }
        public string? Title { get; set; }
//#if (HasCode)
        public string? Code { get; set; }
//#endif
        public bool IsActive { get; set; } = true;

        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }

        /// <summary>Used by the generic catalog service; explicit so it is not part of the JSON contract.</summary>
        long ICatalogUpdateRequest.EntityId => WidgetId;
    }
}
