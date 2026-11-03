namespace EMS.Application.Dtos.User;

public sealed class UpdateUserDto
{
    // null preserves existing assignments; [] clears them.
    public List<long>? RoleIds { get; set; }

    public List<long>? SkillIds { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PersonnelCode { get; set; } = string.Empty;

    public string? NationalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public long? SupportingDepartmentId { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional concurrency token received from GetById.
    /// A mismatch is rejected with HTTP 409.
    /// </summary>
    public string? RowVersion { get; set; }
}
