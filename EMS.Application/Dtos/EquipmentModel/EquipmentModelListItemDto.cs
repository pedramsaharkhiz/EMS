using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentModel
{
    public class EquipmentModelListItemDto
    {
        public long Id { get; set; }
        public long EquipmentTypeId { get; set; }
        public long BrandId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
    }
}
