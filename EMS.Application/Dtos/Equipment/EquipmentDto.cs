using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Equipment
{
    public class EquipmentDto
    {
        public string SerialNumber { get; set; }
        public string AssetNumber { get; set; }
        public string YaasCode { get; set; }
        public long? EquipmentTypeId { get; set; }
        public long? EquipmentModelId { get; set; }
        public long EquipmentStatusId { get; set; }
        public long CurrentEquipmentLocationTypeId { get; set; }
        public long? CurrentOrganizationId { get; set; }
        public long? CurrentSupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
