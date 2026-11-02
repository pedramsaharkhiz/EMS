using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentLocationType
{
    public class EquipmentLocationTypeCreateDto : ICatalogCreateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
