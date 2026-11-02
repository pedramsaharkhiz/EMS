using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class Zone : BaseEntity, IAuditableEntity
    {
        public long ZoneId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public ICollection<OrganizationManagement> OrganizationManagements { get; set; } = new List<OrganizationManagement>();
    }
}
