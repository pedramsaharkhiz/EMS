using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class TechnicianSkillConfiguration : IEntityTypeConfiguration<TechnicianSkill>
    {
        public void Configure(EntityTypeBuilder<TechnicianSkill> builder)
        {
            builder.ToTable("TechnicianSkills");
            builder.HasKey(x => x.TechnicianSkillId);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasOne<ApplicationUser>().WithMany(x => x.TechnicianSkills).HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Skill).WithMany(x => x.TechnicianSkills).HasForeignKey(x => x.SkillId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}