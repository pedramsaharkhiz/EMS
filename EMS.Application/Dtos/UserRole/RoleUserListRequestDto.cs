using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.UserRole;

public sealed class RoleUserListRequestDto : PagedRequest
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}