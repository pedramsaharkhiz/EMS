﻿using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class EquipmentLocationHistory : BaseEntity, IAuditableEntity
    {
        public long EquipmentLocationHistoryId { get; set; }
        public long EquipmentId { get; set; }
        public long EquipmentLocationTypeId { get; set; }
        public long? OrganizationId { get; set; }
        public long? SupportingDepartmentId { get; set; }
        public long? TechnicianUserId { get; set; }
        public DateTime AssignedDateTime { get; set; }
        public long? ChangedByUserId { get; set; }
        public string? Description { get; set; }

        public Equipment Equipment { get; set; } = null!;
        public EquipmentLocationType EquipmentLocationType { get; set; } = null!;
        public Organization? Organization { get; set; }
        public SupportingDepartment? SupportingDepartment { get; set; }
        public ICollection<TechnicianAssignmentAction> TechnicianAssignmentActions { get; set; } = new List<TechnicianAssignmentAction>();
    }
}