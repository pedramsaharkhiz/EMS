using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace EMS.Domain.Entities
{
    public class SupportingDepartment : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long SupportingDepartmentId { get; set; }
        public long CityId { get; set; }
        public string Title { get; set; } = null!;

        public City City { get; set; } = null!;
        /// <summary>The branches this department serves.</summary>
        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
        public ICollection<Equipment> CurrentEquipments { get; set; } = new List<Equipment>();
        public ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();
    }
}