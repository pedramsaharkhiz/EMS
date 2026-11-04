using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;

namespace EMS.Application.Interfaces.Auth;

public interface IUserManagementService
{
    Task<PagedResult<UserListItemDto>> GetPagedAsync(
        UserListRequestDto request,
        CancellationToken cancellationToken = default);
    Task<UserDetailDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto> CreateAsync(
        CreateUserDto request,
        CancellationToken cancellationToken = default);

    Task<UserDetailDto?> UpdateAsync(
        long id,
        UpdateUserDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task SetPasswordAsync(
        long id,
        string newPassword,
        CancellationToken cancellationToken = default);
}
