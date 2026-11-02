using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class EquipmentType : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long EquipmentTypeId { get; set; }
        public long EquipmentCategoryId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public EquipmentCategory EquipmentCategory { get; set; } = null!;
        public ICollection<EquipmentModel> EquipmentModels { get; set; } = new List<EquipmentModel>();
        public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
