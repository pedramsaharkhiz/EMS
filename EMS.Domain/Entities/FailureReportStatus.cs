using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class FailureReportStatus : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long FailureReportStatusId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
    }
}
