using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.EntityAttachment
{
    public class EntityAttachmentDto
    {
        public long TableId { get; set; }
        public long EntityId { get; set; }
        public Guid? StreamId { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
