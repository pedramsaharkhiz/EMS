using EMS.Application.Common.Catalog;

namespace EMS.Application.Dtos.Skill;

public sealed class SkillUpdateDto : ICatalogUpdateRequest
{
    public string? Title { get; set; }

    public string? Code { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public string? RowVersion { get; set; }
}
