namespace EMS.Application.Dtos.User;

public sealed class CreateUserDto
{
    public List<long>? RoleIds { get; set; }

    public List<long>? SkillIds { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PersonnelCode { get; set; } = string.Empty;

    public string? NationalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public long? SupportingDepartmentId { get; set; }

    /// <summary>Optional post (سمت) of the user.</summary>
    public long? PostId { get; set; }

    public bool IsActive { get; set; } = true;
}
