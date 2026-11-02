namespace EMS.Application.Dtos.User;

public sealed class UpdateUserDto
{
    public string UserName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PersonnelCode { get; set; } = string.Empty;

    public string? NationalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public long? SupportingDepartmentId { get; set; }

    public bool IsActive { get; set; } = true;
}