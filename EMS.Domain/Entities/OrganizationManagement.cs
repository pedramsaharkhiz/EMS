using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class OrganizationManagement : BaseEntity, IAuditableEntity
    {
        public long OrganizationManagementId { get; set; }
        public long ZoneId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public Zone Zone { get; set; } = null!;
        public ICollection<Field> Fields { get; set; } = new List<Field>();
        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
    }
}
