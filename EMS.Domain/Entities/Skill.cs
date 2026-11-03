using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities;

public class Skill : BaseEntity, IAuditableEntity, ICatalogEntity
{
    public long SkillId { get; set; }

    public string Title { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public ICollection<TechnicianSkill> TechnicianSkills { get; set; }
        = new List<TechnicianSkill>();
}
