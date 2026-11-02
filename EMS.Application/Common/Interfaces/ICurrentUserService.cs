using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Common.Interfaces
{
    public interface ICurrentUserService 
    { 
        long? UserId { get; } 
        string? UserName { get; } 
        bool IsAuthenticated { get; } 
        IEnumerable<string> Roles { get; } 
        IEnumerable<string> Permissions { get; } 
    }
}
