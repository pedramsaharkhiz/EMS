using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentStatus
{
    public class EquipmentStatusListItemDto
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }
}
