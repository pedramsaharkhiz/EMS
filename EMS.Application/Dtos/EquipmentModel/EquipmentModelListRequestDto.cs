using EMS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentModel
{
    public class EquipmentModelListRequestDto : PagedRequest
    {
        public string? Title { get; set; }
        public string? Code { get; set; }
        public long? EquipmentTypeId { get; set; }
        public long? BrandId { get; set; }
        //public bool IsActive { get; set; } = true;
    }
}
