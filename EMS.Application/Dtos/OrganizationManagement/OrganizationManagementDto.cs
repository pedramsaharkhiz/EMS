using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.OrganizationManagement
{
    public class OrganizationManagementDto
    {
        public string Title { get; set; }
        public long ZoneId { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
    }
}
