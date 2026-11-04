namespace EMS.Application.Dtos.User;

public class UserListItemDto
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string PersonnelCode { get; set; } = string.Empty;

    public string? NationalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public long? SupportingDepartmentId { get; set; }

    /// <summary>The user's post (سمت), optional; the three system posts have the codes Technician, UnitManager and Registrar.</summary>
    public long? PostId { get; set; }

    public string? PostCode { get; set; }

    public string? PostTitle { get; set; }

    public bool IsActive { get; set; }

    public List<UserRoleSummaryDto> Roles { get; set; } = [];

    public List<UserSkillSummaryDto> Skills { get; set; } = [];
}

public class UserRoleSummaryDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class UserSkillSummaryDto
{
    public long SkillId { get; set; }
    public string Title { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}