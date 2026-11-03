using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Role;
using EMS.Application.Dtos.User;
using EMS.Application.Dtos.UserRole;
using EMS.Application.Interfaces.Auth;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

[Authorize]
public sealed class RoleController : BaseApiController
{
    private readonly IRoleService _roleService;
    private readonly IRoleManagementService _roleManagementService;
    private readonly IUserRoleAssignmentService _assignmentService;

    public RoleController(
        IRoleService roleService,
        IRoleManagementService roleManagementService,
        IUserRoleAssignmentService assignmentService)
    {
        _roleService = roleService;
        _roleManagementService = roleManagementService;
        _assignmentService = assignmentService;
    }

    // ---------------------------------------------------------
    // Role CRUD
    // ---------------------------------------------------------

    [Permission(Permissions.Roles.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleListItemDto>>>> GetPaged([FromQuery] RoleListRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _roleManagementService.GetPagedAsync(
            request,
            cancellationToken);

        return Paged(result);
    }

    [Permission(Permissions.Roles.View)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> GetById(long id, CancellationToken cancellationToken)
    {
        var role = await _roleManagementService.GetByIdAsync(
            id,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);
        }

        return Success(role);
    }

    [Permission(Permissions.Roles.Create)]
    [HttpPost]
    [ProducesResponseType<ApiResponse<RoleDetailDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Create([FromBody] CreateRoleDto request, CancellationToken cancellationToken)
    {
        var role = await _roleManagementService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAt(
            nameof(GetById),
            new { id = role.Id },
            role);
    }

    [Permission(Permissions.Roles.Update)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<RoleDetailDto>>> Update(long id, [FromBody] UpdateRoleDto request, CancellationToken cancellationToken)
    {
        var role = await _roleManagementService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);
        }

        return Updated(role);
    }

    [Permission(Permissions.Roles.Delete)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
    {
        var deleted = await _roleManagementService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException("نقش یافت نشد.", ErrorCodes.RoleNotFound);
        }

        return Deleted();
    }

    // ---------------------------------------------------------
    // Existing Permission management
    // ---------------------------------------------------------

    [Permission(Permissions.Roles.ManagePermissions)]
    [HttpPost("AssignPermissions")]
    public async Task<ActionResult<ApiResponse<object>>> AssignPermissions([FromBody] AssignPermissionsToRoleDto request, CancellationToken cancellationToken)
    {
        await _roleService.AssignPermissionsAsync(
            request.RoleName,
            request.Permissions,
            cancellationToken);

        return Done("دسترسی با موفقیت تخصیص یافت.");
    }

    [Permission(Permissions.Roles.View)]
    [HttpGet("{roleName}/permissions")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetPermissions(string roleName, CancellationToken cancellationToken)
    {
        var permissions = await _roleService.GetPermissionsAsync(
            roleName,
            cancellationToken);

        return Success(permissions);
    }

    // ---------------------------------------------------------
    // Role -> Users
    // ---------------------------------------------------------

    [Permission(Permissions.Roles.View)]
    [HttpGet("{roleId:long}/users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserListItemDto>>>> GetUsers(long roleId, [FromQuery] RoleUserListRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetRoleUsersAsync(
            roleId,
            request,
            cancellationToken);

        return Paged(result);
    }

    [Permission(Permissions.Roles.ManageUsers)]
    [HttpPut("{roleId:long}/users")]
    public async Task<ActionResult<ApiResponse<object>>> SetUsers(long roleId, [FromBody] SetRoleUsersDto request, CancellationToken cancellationToken)
    {
        await _assignmentService.SetRoleUsersAsync(
            roleId,
            request.UserIds,
            cancellationToken);

        return Done();
    }

    [Permission(Permissions.Roles.ManageUsers)]
    [HttpPost("{roleId:long}/users/{userId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> AddUser(long roleId, long userId, CancellationToken cancellationToken)
    {
        await _assignmentService.AddUserToRoleAsync(
            roleId,
            userId,
            cancellationToken);

        return Done();
    }

    [Permission(Permissions.Roles.ManageUsers)]
    [HttpDelete("{roleId:long}/users/{userId:long}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveUser(long roleId, long userId, CancellationToken cancellationToken)
    {
        await _assignmentService.RemoveUserFromRoleAsync(
            roleId,
            userId,
            cancellationToken);

        return Done();
    }
}