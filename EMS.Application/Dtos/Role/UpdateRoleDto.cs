namespace EMS.Application.Dtos.Role;

public sealed class UpdateRoleDto
{
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}