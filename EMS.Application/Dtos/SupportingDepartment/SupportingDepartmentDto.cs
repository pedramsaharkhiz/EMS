using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentDto
    {
        public long CityId { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
    }
}