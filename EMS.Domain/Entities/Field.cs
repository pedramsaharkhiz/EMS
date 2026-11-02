using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class Field : BaseEntity, IAuditableEntity
    {
        public long FieldId { get; set; }
        public long OrganizationManagementId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public OrganizationManagement OrganizationManagement { get; set; } = null!;
        public ICollection<Organization> Organizations { get; set; } = new List<Organization>();
    }
}
