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
            builder.HasSoftDeleteFilter();
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode().IsRequired(false);
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_EquipmentTypes_Title");
            builder.HasOne(x => x.EquipmentCategory).WithMany(x => x.EquipmentTypes).HasForeignKey(x => x.EquipmentCategoryId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}