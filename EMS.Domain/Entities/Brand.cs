using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class Brand : BaseEntity, IAuditableEntity
    {
        public long BrandId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public ICollection<EquipmentModel> EquipmentModels { get; set; } = new List<EquipmentModel>();
    }
}
