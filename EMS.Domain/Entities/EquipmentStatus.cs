using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class EquipmentStatus : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long EquipmentStatusId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
