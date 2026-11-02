using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Common.Security
{
    public sealed record TokenUserDto(long Id,
        string UserName,
        string Email,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<string> Permissions,
        string? FirstName = null,
        string? LastName = null);
}
