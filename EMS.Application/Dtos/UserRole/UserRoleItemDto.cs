namespace EMS.Application.Dtos.UserRole;

public sealed class UserRoleItemDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}