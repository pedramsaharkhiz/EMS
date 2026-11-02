using EMS.Application.Common.Models;
using EMS.Application.Dtos.Role;

namespace EMS.Application.Interfaces.Auth;

public interface IRoleManagementService
{
    Task<PagedResult<RoleListItemDto>> GetPagedAsync(RoleListRequestDto request, CancellationToken cancellationToken = default);

    Task<RoleDetailDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<RoleDetailDto> CreateAsync(CreateRoleDto request, CancellationToken cancellationToken = default);

    Task<RoleDetailDto?> UpdateAsync(long id, UpdateRoleDto request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}