using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class TechnicianSkill : BaseEntity, IAuditableEntity
    {
        public long TechnicianSkillId { get; set; }
        public long TechnicianUserId { get; set; }
        public long SkillId { get; set; }

        public Skill Skill { get; set; } = null!;
    }
}