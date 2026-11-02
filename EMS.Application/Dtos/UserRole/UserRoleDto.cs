using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Dtos.UserRole
{
    public class UserRoleDto
    {
        public long UserId { get; set; }
        public long RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
