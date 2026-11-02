using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.FailureCategory
{
    public class FailureCategoryUpdateDto
    {
        public long FailureCategoryId { get; set; }
        public string? Title { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
