namespace EMS.Application.Dtos.UserRole;

public sealed class SetUserRolesDto
{
    public List<long> RoleIds { get; set; } = new();
}