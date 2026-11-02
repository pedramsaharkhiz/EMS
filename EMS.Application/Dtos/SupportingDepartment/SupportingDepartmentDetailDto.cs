using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.SupportingDepartment
{
    public class SupportingDepartmentDetailDto
    {
        public long SupportingDepartmentId { get; set; }
        public long CityId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        /// <summary>Send this value back in the update request to detect concurrent modifications (409).</summary>
        public string RowVersion { get; set; } = "0";
    }
}