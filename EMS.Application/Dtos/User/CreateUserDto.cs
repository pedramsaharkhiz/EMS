namespace EMS.Application.Dtos.User;

public sealed class CreateUserDto
{
    public List<long>? RoleIds { get; set; }

    public List<long>? SkillIds { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    private string _personnelCode = string.Empty;
    public string PersonnelCode { get => _personnelCode; set => _personnelCode = EMS.Domain.UserIdentityRules.Normalize(value); }

    private string? _nationalCode;
    public string? NationalCode { get => _nationalCode; set => _nationalCode = EMS.Domain.UserIdentityRules.NormalizeOptional(value); }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    /// <summary>The supporting departments the user works for (zero, one or several; a repeated id counts once). An update replaces the whole list.</summary>
    public List<long>? SupportingDepartmentIds { get; set; }

    /// <summary>Optional post (سمت) of the user.</summary>
    public long? PostId { get; set; }

    public bool IsActive { get; set; } = true;
}
