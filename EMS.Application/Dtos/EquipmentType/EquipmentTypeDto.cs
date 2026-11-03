using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentType
{
    public class EquipmentTypeDto
    {
        public long EquipmentCategoryId { get; set; }
        public string Title { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
    }
}
