using EMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Entities
{
    public class EntityAttachment : BaseEntity, IAuditableEntity
    {
        public long EntityAttachmentId { get; set; }
        public long TableId { get; set; }
        public long EntityId { get; set; }
        public Guid? StreamId { get; set; }
        public string? Description { get; set; }

        public Table Table { get; set; } = null!;
    }
}
