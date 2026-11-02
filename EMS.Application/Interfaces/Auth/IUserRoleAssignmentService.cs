using EMS.Application.Common.Models;
using EMS.Application.Dtos.User;
using EMS.Application.Dtos.UserRole;

namespace EMS.Application.Interfaces.Auth;

public interface IUserRoleAssignmentService
{
    // عملیات از سمت کاربر

    Task<IReadOnlyList<UserRoleItemDto>> GetUserRolesAsync(
        long userId,
        CancellationToken cancellationToken = default);

    Task SetUserRolesAsync(
        long userId,
        IReadOnlyCollection<long> roleIds,
        CancellationToken cancellationToken = default);

    Task AddRoleToUserAsync(
        long userId,
        long roleId,
        CancellationToken cancellationToken = default);

    Task RemoveRoleFromUserAsync(
        long userId,
        long roleId,
        CancellationToken cancellationToken = default);


    // عملیات از سمت نقش

    Task<PagedResult<UserListItemDto>> GetRoleUsersAsync(
        long roleId,
        RoleUserListRequestDto request,
        CancellationToken cancellationToken = default);

    Task SetRoleUsersAsync(
        long roleId,
        IReadOnlyCollection<long> userIds,
        CancellationToken cancellationToken = default);

    Task AddUserToRoleAsync(
        long roleId,
        long userId,
        CancellationToken cancellationToken = default);

    Task RemoveUserFromRoleAsync(
        long roleId,
        long userId,
        CancellationToken cancellationToken = default);
}