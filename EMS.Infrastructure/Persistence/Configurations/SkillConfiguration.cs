using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations;

public sealed class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.ToTable("Skills");

        builder.HasKey(x => x.SkillId);

        builder.HasSoftDeleteFilter();

        builder.Property(x => x.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired(false);

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.CreatedByUserId)
            .IsRequired(false);

        builder.Property(x => x.ModifiedDateTime)
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Title)
            .IsUnique()
            .HasFilter("[IsActive] = 1")
            .HasDatabaseName("UX_Skills_Title");

        builder.HasIndex(x => x.Code)
            .HasDatabaseName("IX_Skills_Code");
    }
}
