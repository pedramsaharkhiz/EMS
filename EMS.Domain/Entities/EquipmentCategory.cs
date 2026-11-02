using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class EquipmentCategory : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long EquipmentCategoryId { get; set; }
        public string Title { get; set; } = null!;
        public ICollection<EquipmentType> EquipmentTypes { get; set; } = new List<EquipmentType>();
    }
}
