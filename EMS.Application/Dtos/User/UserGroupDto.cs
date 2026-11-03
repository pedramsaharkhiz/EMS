namespace EMS.Application.Dtos.User;

public sealed class UserGroupDto
{
    // null: no active role/skill, or no department.
    // For isActive grouping: 1 = active, 0 = inactive.
    public long? Key { get; set; }

    public string Name { get; set; } = string.Empty;

    public int UserCount { get; set; }
}
