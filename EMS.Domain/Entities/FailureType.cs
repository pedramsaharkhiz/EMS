using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class FailureType : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long FailureTypeId { get; set; }
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public FailureCategory FailureCategory { get; set; } = null!;
        /// <summary>The equipment types this failure type applies to. Empty = it applies to every equipment type.</summary>
        public ICollection<FailureTypeEquipmentType> EquipmentTypes { get; set; } = new List<FailureTypeEquipmentType>();
        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
    }
}
