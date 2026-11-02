using EMS.Application.Common.Logging;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Identity.Security
{
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ISecurityEventLogger _securityLog;

        public PermissionAuthorizationHandler(ISecurityEventLogger securityLog)
        {
            _securityLog = securityLog;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var hasPermission = context.User.Claims.Any(x => x.Type == "permission" && x.Value == requirement.Permission);
            if (hasPermission)
                context.Succeed(requirement);
            else if (context.User.Identity?.IsAuthenticated == true)
                // Authenticated but lacking the permission => 403. (Anonymous callers get a 401 challenge instead.)
                _securityLog.AccessDenied(requirement.Permission);
            return Task.CompletedTask;
        }
    }
}
