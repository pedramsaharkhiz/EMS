using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class Table : BaseEntity, IAuditableEntity
    {
        public long TableId { get; set; }
        public string TableName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public bool IsAttachmentEnabled { get; set; }
        public ICollection<EntityAttachment> EntityAttachments { get; set; } = new List<EntityAttachment>();
    }
}
