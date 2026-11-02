using System;
using System.Collections.Generic;
using System.Text;

using EMS.Application.Common.Catalog;
namespace EMS.Application.Dtos.EquipmentModel
{
    public class EquipmentModelUpdateDto : ICatalogUpdateRequest
    {
        public long EquipmentModelId { get; set; }
        public long? EquipmentTypeId { get; set; }
        public long? BrandId { get; set; }
        public string? Title { get; set; }
        public string? Code { get; set; }
        public bool IsActive { get; set; } = true;
        /// <summary>Optional: the RowVersion read together with the record. A mismatch is rejected with 409.</summary>
        public string? RowVersion { get; set; }

        /// <summary>Used by the generic catalog service; explicit so it is not part of the JSON contract.</summary>
        long ICatalogUpdateRequest.EntityId => EquipmentModelId;
    }
}
