namespace EMS.Application.Dtos.Skill;

public sealed class SkillGroupDto
{
    public string? Key { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SkillCount { get; set; }
}
