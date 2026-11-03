namespace EMS.Application.Dtos.Skill;

public sealed class SkillListItemDto
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int TechnicianCount { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime? ModifiedDateTime { get; set; }
}
