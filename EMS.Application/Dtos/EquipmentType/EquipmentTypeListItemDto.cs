using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EquipmentType
{
    public class EquipmentTypeListItemDto
    {
        public long Id { get; set; }
        public long EquipmentCategoryId { get; set; }
        public string? EquipmentCategoryTitle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public bool IsActive { get; set; }
        /// <summary>True when the type has at least one ACTIVE model (the same meaning as <c>without-models</c>, which lists the types where this is false).</summary>
        public bool HasModels { get; set; }
    }
}
