using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentListItemDto
    {
        public long Id { get; set; }
        public long CityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}