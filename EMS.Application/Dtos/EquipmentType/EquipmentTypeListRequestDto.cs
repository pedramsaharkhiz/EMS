using EMS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentType
{
    public class EquipmentTypeListRequestDto : PagedRequest, ICatalogListRequest
    {
        public string? Title { get; set; }
        public string? Code { get; set; }
        public long? EquipmentCategoryId { get; set; }
        //public bool IsActive { get; set; } = true;
        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}
