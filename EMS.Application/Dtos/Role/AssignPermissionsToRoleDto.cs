using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.Role
{
    public class AssignPermissionsToRoleDto 
    { 
        public string RoleName { get; set; } = null!;
        public List<string> Permissions { get; set; } = new();
    }
}
