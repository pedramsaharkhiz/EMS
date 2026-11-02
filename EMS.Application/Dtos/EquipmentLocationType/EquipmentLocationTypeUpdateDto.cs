using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentLocationType
{
    public class EquipmentLocationTypeUpdateDto
    {
        public long EquipmentLocationTypeId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
