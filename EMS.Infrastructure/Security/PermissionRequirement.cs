using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Security
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission) 
        { 
            Permission = permission; 
        }
    }
}
