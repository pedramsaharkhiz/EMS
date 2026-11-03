using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Table
{
    public class TableDto
    {
        public string TableName { get; set; }
        public string DisplayName { get; set; }
        public bool IsAttachmentEnabled { get; set; }
        public bool IsActive { get; set; }
    }
}
