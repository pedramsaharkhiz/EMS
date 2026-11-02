using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentTypeCreateDto
    {
        public long EquipmentCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
