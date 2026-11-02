using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class OrganizationType : BaseEntity, IAuditableEntity
    {
        public long OrganizationTypeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
    }
}
