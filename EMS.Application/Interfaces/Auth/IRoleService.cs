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

        /// <summary>Every permission of the system, grouped, with a mark on those the role holds (404 <c>role_not_found</c>).</summary>
        Task<EMS.Application.Dtos.Permission.RolePermissionsDto> GetRolePermissionsAsync(long roleId, CancellationToken cancellationToken = default);

        /// <summary>Replaces the role's permissions with exactly the given list and returns the new state. The administrator role is refused (409 <c>protected_record</c>).</summary>
        Task<EMS.Application.Dtos.Permission.RolePermissionsDto> SetRolePermissionsAsync(long roleId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);
    }
}
