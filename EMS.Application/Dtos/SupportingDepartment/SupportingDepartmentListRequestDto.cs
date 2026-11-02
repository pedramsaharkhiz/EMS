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
        /// <summary>null = active records only (default); true/false = only active / only inactive records.</summary>
        public bool? IsActive { get; set; }
    }
}