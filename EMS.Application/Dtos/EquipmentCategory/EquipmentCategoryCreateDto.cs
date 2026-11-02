using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentCategoryCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
    }
}
