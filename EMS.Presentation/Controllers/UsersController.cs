using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.User;
using EMS.Application.Dtos.UserRole;
using EMS.Application.Interfaces.Auth;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

public sealed class UsersController : BaseApiController
{
    private readonly IAuthorizationService _authorization;
    private readonly IUserManagementService _users;
    private readonly IUserRoleAssignmentService _assignmentService;

    public UsersController(
        IUserManagementService users,
        IUserRoleAssignmentService assignmentService,
        IAuthorizationService authorization)
    {
        _authorization = authorization;
        _users = users;
        _assignmentService = assignmentService;
    }

    // GET: /api/Users
    [Permission(Permissions.Users.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserListItemDto>>>>
        GetPaged(
            [FromQuery] UserListRequestDto request,
            CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.GroupBy))
        {
            throw new BadRequestException(
                "برای گروه‌بندی از /api/Users/groups استفاده کنید.");
        }

        var result = await _users.GetPagedAsync(
            request,
            cancellationToken);

        return Paged(result);
    }

    // GET: /api/Users/groups
    // Counts cover all matching users; pagination is over groups.
    [Permission(Permissions.Users.View)]
    [HttpGet("groups")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserGroupDto>>>>
        GetGrouped(
            [FromQuery] UserListRequestDto request,
            CancellationToken cancellationToken)
    {
        var result = await _users.GetGroupedAsync(
            request,
            cancellationToken);

        return Paged(result);
    }

    // GET: /api/Users/10
    [Permission(Permissions.Users.View)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(
            id,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "کاربر موردنظر پیدا نشد.",
                ErrorCodes.UserNotFound);
        }

        return Success(user);
    }

    // POST: /api/Users
    [Permission(Permissions.Users.Create)]
    [HttpPost]
    [ProducesResponseType<ApiResponse<UserDetailDto>>(
        StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> Create(
        [FromBody] CreateUserDto request,
        CancellationToken cancellationToken)
    {
        // Role assignment requires Users.ManageRoles as well.
        if (request.RoleIds is not null &&
            !(await _authorization.AuthorizeAsync(
                User,
                $"Permission:{Permissions.Users.ManageRoles}")).Succeeded)
        {
            return Forbid();
        }

        var user = await _users.CreateAsync(
            request,
            cancellationToken);

        return CreatedAt(
            nameof(GetById),
            new { id = user.Id },
            user);
    }

    // PUT: /api/Users/10
    [Permission(Permissions.Users.Update)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> Update(
        long id,
        [FromBody] UpdateUserDto request,
        CancellationToken cancellationToken)
    {
        // Even [] changes assignments and needs ManageRoles.
        if (request.RoleIds is not null &&
            !(await _authorization.AuthorizeAsync(
                User,
                $"Permission:{Permissions.Users.ManageRoles}")).Succeeded)
        {
            return Forbid();
        }

        var user = await _users.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "کاربر موردنظر پیدا نشد.",
                ErrorCodes.UserNotFound);
        }

        return Updated(user);
    }

    // PUT: /api/Users/10/password
    [Permission(Permissions.Users.Update)]
    [HttpPut("{id:long}/password")]
    public async Task<ActionResult<ApiResponse<object>>> SetPassword(
        long id,
        [FromBody] SetUserPasswordDto request,
        CancellationToken cancellationToken)
    {
        await _users.SetPasswordAsync(
            id,
            request.NewPassword,
            cancellationToken);

        return Done("کلمه عبور با موفقیت تغییر کرد.");
    }

    // DELETE: /api/Users/10
    // Logical deletion: deactivate the user.
    [Permission(Permissions.Users.Delete)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var deleted = await _users.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException(
                "کاربر موردنظر پیدا نشد.",
                ErrorCodes.UserNotFound);
        }

        return Deleted("کاربر با موفقیت غیرفعال شد.");
    }

    // GET: /api/Users/10/roles
    [Permission(Permissions.Users.View)]
    [HttpGet("{userId:long}/roles")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserRoleItemDto>>>>
        GetRoles(
            long userId,
            CancellationToken cancellationToken)
    {
        var roles = await _assignmentService.GetUserRolesAsync(
            userId,
            cancellationToken);

        return Success(roles);
    }

    // PUT: /api/Users/10/roles
    [Permission(Permissions.Users.ManageRoles)]
    [HttpPut("{userId:long}/roles")]
    public async Task<ActionResult<ApiResponse<object>>> SetRoles(
        long userId,
        [FromBody] SetUserRolesDto request,
        CancellationToken cancellationToken)
    {
        await _assignmentService.SetUserRolesAsync(
            userId,
            request.RoleIds,
            cancellationToken);

        return Done();
    }

    // POST: /api/Users/10/roles/2
    [Permission(Permissions.Users.ManageRoles)]
    [HttpPost("{userId:long}/roles/{roleId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> AddRole(
        long userId,
        long roleId,
        CancellationToken cancellationToken)
    {
        await _assignmentService.AddRoleToUserAsync(
            userId,
            roleId,
            cancellationToken);

        return Done();
    }

    // DELETE: /api/Users/10/roles/2
    [Permission(Permissions.Users.ManageRoles)]
    [HttpDelete("{userId:long}/roles/{roleId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveRole(
        long userId,
        long roleId,
        CancellationToken cancellationToken)
    {
        await _assignmentService.RemoveRoleFromUserAsync(
            userId,
            roleId,
            cancellationToken);

        return Done();
    }
}
