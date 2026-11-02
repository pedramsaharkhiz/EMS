using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class EquipmentLocationType : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long EquipmentLocationTypeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public ICollection<Equipment> CurrentEquipments { get; set; } = new List<Equipment>();
        public ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();
    }
}
