using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.FailureType
{
    public class FailureTypeDto
    {
        public long? FailureCategoryId { get; set; }
        public string Title { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
    }
}
