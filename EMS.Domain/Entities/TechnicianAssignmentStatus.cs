using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class TechnicianAssignmentStatus : BaseEntity, IAuditableEntity
    {
        public long TechnicianAssignmentStatusId { get; set; }
        public string Title { get; set; } = null!;
        public ICollection<TechnicianAssignment> TechnicianAssignments { get; set; } = new List<TechnicianAssignment>();
    }
}
