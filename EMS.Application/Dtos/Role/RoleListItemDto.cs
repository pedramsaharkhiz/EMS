namespace EMS.Application.Dtos.Role;

public class RoleListItemDto
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public int UserCount { get; set; }

    /// <summary>The administrator role: always holds every permission; its name, state and permissions cannot be changed and it cannot be deleted.</summary>
    public bool IsSystem => EMS.Application.Common.Security.SystemRoles.IsSystem(Name);
}