using EMS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentCategoryListRequestDto : PagedRequest
    {
        public string? Title { get; set; }
        //public bool IsActive { get; set; } = true;
    }
}
