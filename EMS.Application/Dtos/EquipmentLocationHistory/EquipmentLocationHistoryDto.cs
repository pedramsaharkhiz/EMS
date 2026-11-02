using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentLocationHistory
{
    public class EquipmentLocationHistoryDto
    {
        public long EquipmentId { get; set; }
        public long EquipmentLocationTypeId { get; set; }
        public long? OrganizationId { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public long? ChangedByUserId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
