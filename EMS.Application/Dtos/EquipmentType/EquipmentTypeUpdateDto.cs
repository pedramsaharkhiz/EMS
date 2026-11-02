using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentTypeUpdateDto
    {
        public long EquipmentTypeId { get; set; }
        public long? EquipmentCategoryId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
