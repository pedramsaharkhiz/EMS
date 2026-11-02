using EMS.Domain;
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
            builder.HasSoftDeleteFilter();
            builder.HasKey(x => x.EquipmentModelId);
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_EquipmentModels_Title");
            builder.HasOne(x => x.EquipmentType).WithMany(x => x.EquipmentModels).HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Brand).WithMany(x => x.EquipmentModels).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}