using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Role
{
    public class RoleDto
    {
        public string Name { get; set; }
        public string NormalizedName { get; set; }
        public bool IsActive { get; set; }
    }
}
