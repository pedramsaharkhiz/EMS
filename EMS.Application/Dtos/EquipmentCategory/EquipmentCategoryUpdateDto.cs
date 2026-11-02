using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentCategory
{
    public class EquipmentCategoryUpdateDto : ICatalogUpdateRequest
    {
        public long EquipmentCategoryId { get; set; }
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }

        /// <summary>Used by the generic catalog service; explicit so it is not part of the JSON contract.</summary>
        long ICatalogUpdateRequest.EntityId => EquipmentCategoryId;
    }
}
