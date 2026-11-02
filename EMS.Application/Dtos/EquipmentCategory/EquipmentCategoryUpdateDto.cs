using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentCategoryUpdateDto
    {
        public long EquipmentCategoryId { get; set; }
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
