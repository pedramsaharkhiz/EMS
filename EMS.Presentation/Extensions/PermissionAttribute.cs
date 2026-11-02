using Microsoft.AspNetCore.Authorization;

namespace EMS.Presentation.Extensions
{
    public class PermissionAttribute : AuthorizeAttribute
    {
        public PermissionAttribute(string permission) 
        { 
            Policy = $"Permission:{permission}"; 
        }
    }
}
