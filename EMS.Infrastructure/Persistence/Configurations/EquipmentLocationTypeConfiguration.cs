using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentLocationTypeConfiguration : IEntityTypeConfiguration<EquipmentLocationType>
    {
        public void Configure(EntityTypeBuilder<EquipmentLocationType> builder)
        {
            builder.ToTable("EquipmentLocationTypes");
            builder.HasKey(x => x.EquipmentLocationTypeId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasIndex(x => x.Title).IsUnique().HasDatabaseName("UX_EquipmentLocationTypes_Title");
        }
    }
}