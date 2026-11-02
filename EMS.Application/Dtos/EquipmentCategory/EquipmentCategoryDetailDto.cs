using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentCategoryDetailDto
    {
        public long EquipmentCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
