using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.Role;

public sealed class RoleListRequestDto : PagedRequest
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }
}