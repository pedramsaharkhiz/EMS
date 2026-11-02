using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class EquipmentModel : BaseEntity, IAuditableEntity
    {
        public long EquipmentModelId { get; set; }
        public long EquipmentTypeId { get; set; }
        public long BrandId { get; set; }

        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public EquipmentType EquipmentType { get; set; } = null!;
        public Brand Brand { get; set; } = null!;
        public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
