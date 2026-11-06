using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentModel
{
    public class EquipmentModelDetailDto
    {
        public long Id { get; set; }
        public long EquipmentTypeId { get; set; }
        public string? EquipmentTypeTitle { get; set; }
        public long BrandId { get; set; }
        public string? BrandTitle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}
