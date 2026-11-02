using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentStatusConfiguration : IEntityTypeConfiguration<EquipmentStatus>
    {
        public void Configure(EntityTypeBuilder<EquipmentStatus> builder)
        {
            builder.ToTable("EquipmentStatuses");
            builder.HasKey(x => x.EquipmentStatusId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasIndex(x => x.Title).IsUnique().HasDatabaseName("UX_EquipmentStatuses_Title");
        }
    }
}