using EMS.Application.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentListRequestDto : PagedRequest
    {
        public long? CityId { get; set; }
        public string? Title { get; set; }
        //public bool IsActive { get; set; } = true;
    }
}