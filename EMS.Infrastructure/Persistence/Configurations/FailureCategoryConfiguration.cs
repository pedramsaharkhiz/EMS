using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class FailureCategoryConfiguration : IEntityTypeConfiguration<FailureCategory>
    {
        public void Configure(EntityTypeBuilder<FailureCategory> builder)
        {
            builder.ToTable("FailureCategories");
            builder.HasKey(x => x.FailureCategoryId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasIndex(x => x.Title).IsUnique().HasDatabaseName("UX_FailureCategories_Title");
        }
    }
}