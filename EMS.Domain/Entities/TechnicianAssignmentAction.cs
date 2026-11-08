using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class TechnicianAssignmentAction : BaseEntity, IAuditableEntity
    {
        public long TechnicianAssignmentActionId { get; set; }
        public long TechnicianAssignmentId { get; set; }
        public long ActionId { get; set; }
        public long? EquipmentLocationHistoryId { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string? Description { get; set; }

        public TechnicianAssignment TechnicianAssignment { get; set; } = null!;
        public ActionEntity Action { get; set; } = null!;
        public EquipmentLocationHistory? EquipmentLocationHistory { get; set; }
    }
}
