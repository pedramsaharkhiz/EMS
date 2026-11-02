using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class FailureReportAction : BaseEntity, IAuditableEntity
    {
        public long FailureReportActionId { get; set; }
        public long FailureReportId { get; set; }
        public long ActionId { get; set; }

        public FailureReport FailureReport { get; set; } = null!;
        public ActionEntity Action { get; set; } = null!;
    }
}