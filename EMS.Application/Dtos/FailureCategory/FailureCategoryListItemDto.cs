using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.FailureCategory
{
    public class FailureCategoryListItemDto
    {
        public long FailureCategoryId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
