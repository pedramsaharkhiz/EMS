using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentTypeConfiguration : IEntityTypeConfiguration<EquipmentType>
    {
        public void Configure(EntityTypeBuilder<EquipmentType> builder)
        {
            builder.ToTable("EquipmentTypes");
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode().IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasIndex(x => x.Title).IsUnique().HasDatabaseName("UX_EquipmentTypes_Title");
            builder.HasOne(x => x.EquipmentCategory).WithMany(x => x.EquipmentTypes).HasForeignKey(x => x.EquipmentCategoryId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}