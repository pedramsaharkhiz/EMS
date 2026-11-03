using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.ActionEntity
{
    public class ActionEntityDto
    {
        public string Title { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
