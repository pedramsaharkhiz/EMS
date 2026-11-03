using EMS.Domain;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class CityConfiguration : IEntityTypeConfiguration<City>
    {
        public void Configure(EntityTypeBuilder<City> builder)
        {
            builder.ToTable("Cities");
            builder.HasSoftDeleteFilter();
            builder.HasKey(x => x.CityId);
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.CreatedByUserId).IsRequired(false);   // master data seeded without a user
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // A city title is unique only within its province (two provinces may both have a city called "Kheirabad").
            builder.HasIndex(x => new { x.ProvinceId, x.Title }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_Cities_ProvinceId_Title");
            builder.HasOne(x => x.Province).WithMany(x => x.Cities).HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
