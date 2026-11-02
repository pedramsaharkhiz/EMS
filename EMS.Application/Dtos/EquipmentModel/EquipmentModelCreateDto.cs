using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentModel
{
    public class EquipmentModelCreateDto : ICatalogCreateRequest
    {
        public long EquipmentTypeId { get; set; }
        public long BrandId { get; set; }
        public string Title { get; set; }
        public string? Code { get; set; }
    }
}
