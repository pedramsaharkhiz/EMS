using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentModelConfiguration : IEntityTypeConfiguration<EquipmentModel>
    {
        public void Configure(EntityTypeBuilder<EquipmentModel> builder)
        {
            builder.ToTable("EquipmentModels");
            builder.HasKey(x => x.EquipmentModelId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasIndex(x => x.Title).IsUnique().HasDatabaseName("UX_EquipmentModels_Title");
            builder.HasOne(x => x.EquipmentType).WithMany(x => x.EquipmentModels).HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Brand).WithMany(x => x.EquipmentModels).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}