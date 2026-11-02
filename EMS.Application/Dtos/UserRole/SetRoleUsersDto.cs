namespace EMS.Application.Dtos.UserRole;

public sealed class SetRoleUsersDto
{
    public List<long> UserIds { get; set; } = new();
}