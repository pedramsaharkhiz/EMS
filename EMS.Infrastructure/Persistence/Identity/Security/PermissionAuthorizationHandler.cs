using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Identity.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var hasPermission = context.User.Claims.Any(x => x.Type == "permission" && x.Value == requirement.Permission);
            if (hasPermission) 
                context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
