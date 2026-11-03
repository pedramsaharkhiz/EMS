using Microsoft.AspNetCore.Authorization;

namespace EMS.Presentation.Security
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission) 
        { 
            Policy = $"Permission:{permission}"; 
        }
    }
}
