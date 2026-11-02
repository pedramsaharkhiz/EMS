using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentModel
{
    public class EquipmentModelCreateDto
    {
        public long EquipmentTypeId { get; set; }
        public long BrandId { get; set; }
        public string Title { get; set; }
        public string? Code { get; set; }
    }
}
