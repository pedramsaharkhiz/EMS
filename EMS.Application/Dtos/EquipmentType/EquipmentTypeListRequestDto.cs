using EMS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentTypeListRequestDto : PagedRequest
    {
        public string? Title { get; set; }
        public string? Code { get; set; }
        public long? EquipmentCategoryId { get; set; }
        //public bool IsActive { get; set; } = true;
    }
}
