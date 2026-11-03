using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.TechnicianAssignmentAction
{
    public class TechnicianAssignmentActionDto
    {
        public long TechnicianAssignmentId { get; set; }
        public long ActionId { get; set; }
        public long EquipmentLocationHistoryId { get; set; }
        public DateTime ActionDateTime { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
