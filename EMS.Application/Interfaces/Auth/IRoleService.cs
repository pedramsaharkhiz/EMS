using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Application.Interfaces.Auth
{
    public interface IRoleService
    {
        Task CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);
        Task AssignPermissionsAsync(string roleName, IEnumerable<string> permissions, CancellationToken cancellationToken = default);
        Task<List<string>> GetPermissionsAsync(string roleName, CancellationToken cancellationToken = default);
    }
}
