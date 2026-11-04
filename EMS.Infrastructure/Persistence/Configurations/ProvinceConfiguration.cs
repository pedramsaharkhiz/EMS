using EMS.Domain;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
    {
        public void Configure(EntityTypeBuilder<Province> builder)
        {
            builder.ToTable("Provinces");
            builder.HasSoftDeleteFilter();
            builder.HasKey(x => x.ProvinceId);
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.CreatedByUserId).IsRequired(false);   // master data seeded without a user
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // Only active records reserve a title.
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_Provinces_Title");
        }
    }
}
