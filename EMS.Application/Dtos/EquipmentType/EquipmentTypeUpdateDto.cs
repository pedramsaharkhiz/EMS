using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentType
{
    public class EquipmentTypeUpdateDto : ICatalogUpdateRequest
    {
        public long? EquipmentCategoryId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }
    }
}
