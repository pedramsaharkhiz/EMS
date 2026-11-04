using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.User;

public sealed class UserListRequestDto : PagedRequest
{
    public long? RoleId { get; set; }

    public long? SkillId { get; set; }

    public bool? HasRole { get; set; }

    public bool? HasSkill { get; set; }

    public string? Search { get; set; }

    public long? Id { get; set; }

    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PersonnelCode { get; set; }

    public string? NationalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public long? SupportingDepartmentId { get; set; }

    public long? PostId { get; set; }

    public bool? IsActive { get; set; }
}
