namespace EMS.Application.Dtos.Skill;

public sealed class SkillDetailDto
{
    public long Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDateTime { get; set; }

    public DateTime? ModifiedDateTime { get; set; }

    public string RowVersion { get; set; } = string.Empty;
}
