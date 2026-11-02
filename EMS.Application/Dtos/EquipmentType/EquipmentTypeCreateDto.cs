using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentType
{
    public class EquipmentTypeCreateDto : ICatalogCreateRequest
    {
        public long EquipmentCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
