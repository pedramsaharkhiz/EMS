using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.User;
using EMS.Application.Dtos.UserRole;
using EMS.Application.Interfaces.Auth;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

public sealed class UsersController : BaseApiController
{
    private readonly IUserManagementService _users;
    private readonly IUserRoleAssignmentService _assignmentService;

    public UsersController(IUserManagementService users, IUserRoleAssignmentService assignmentService)
    {
        _users = users;
        _assignmentService = assignmentService;
    }

    // GET: /api/Users
    [Permission(Permissions.Users.View)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetPaged([FromQuery] UserListRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _users.GetPagedAsync(request,cancellationToken);

        return Ok(result);
    }

    // GET: /api/Users/10
    [Permission(Permissions.Users.View)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<UserDetailDto>> GetById(long id, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(id,cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("کاربر موردنظر پیدا نشد.", ErrorCodes.UserNotFound);
        }

        return Ok(user);
    }

    // POST: /api/Users
    [Permission(Permissions.Users.Create)]
    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> Create([FromBody] CreateUserDto request, CancellationToken cancellationToken)
    {
        var user = await _users.CreateAsync(request,cancellationToken);

        return CreatedAtAction(nameof(GetById),new { id = user.Id },user);
    }

    // PUT: /api/Users/10
    [Permission(Permissions.Users.Update)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<UserDetailDto>> Update(long id, [FromBody] UpdateUserDto request, CancellationToken cancellationToken)
    {
        var user = await _users.UpdateAsync(id,request,cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("کاربر موردنظر پیدا نشد.", ErrorCodes.UserNotFound);
        }

        return Ok(user);
    }

    // PUT: /api/Users/10/password
    [Permission(Permissions.Users.Update)]
    [HttpPut("{id:long}/password")]
    public async Task<IActionResult> SetPassword(long id, [FromBody] SetUserPasswordDto request, CancellationToken cancellationToken)
    {
        await _users.SetPasswordAsync(id,request.NewPassword,cancellationToken);

        return NoContent();
    }

    // DELETE: /api/Users/10
    // این endpoint حذف فیزیکی انجام نمی‌دهد؛ کاربر را غیرفعال می‌کند.
    [Permission(Permissions.Users.Delete)]
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var deleted = await _users.DeleteAsync(id,cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException("کاربر موردنظر پیدا نشد.", ErrorCodes.UserNotFound);
        }

        return NoContent();
    }

    // ---------------------------------------------------------
    // User -> Roles
    // ---------------------------------------------------------

    [Permission(Permissions.Users.View)]
    [HttpGet("{userId:long}/roles")]
    public async Task<ActionResult<IReadOnlyList<UserRoleItemDto>>>
        GetRoles(
            long userId,
            CancellationToken cancellationToken)
    {
        var roles = await _assignmentService.GetUserRolesAsync(
            userId,
            cancellationToken);

        return Ok(roles);
    }

    [Permission(Permissions.Users.ManageRoles)]
    [HttpPut("{userId:long}/roles")]
    public async Task<IActionResult> SetRoles(
        long userId,
        [FromBody] SetUserRolesDto request,
        CancellationToken cancellationToken)
    {
        await _assignmentService.SetUserRolesAsync(
            userId,
            request.RoleIds,
            cancellationToken);

        return NoContent();
    }

    [Permission(Permissions.Users.ManageRoles)]
    [HttpPost("{userId:long}/roles/{roleId:long}")]
    public async Task<IActionResult> AddRole(
        long userId,
        long roleId,
        CancellationToken cancellationToken)
    {
        await _assignmentService.AddRoleToUserAsync(
            userId,
            roleId,
            cancellationToken);

        return NoContent();
    }

    [Permission(Permissions.Users.ManageRoles)]
    [HttpDelete("{userId:long}/roles/{roleId:long}")]
    public async Task<IActionResult> RemoveRole(
        long userId,
        long roleId,
        CancellationToken cancellationToken)
    {
        await _assignmentService.RemoveRoleFromUserAsync(
            userId,
            roleId,
            cancellationToken);

        return NoContent();
    }
}

