using EMS.Domain;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class FailureTypeConfiguration : IEntityTypeConfiguration<FailureType>
    {
        public void Configure(EntityTypeBuilder<FailureType> builder)
        {
            builder.ToTable("FailureTypes");
            builder.HasKey(x => x.FailureTypeId);

            builder.HasSoftDeleteFilter();   // required for catalog entities (see SoftDelete)
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // A title only has to be unique within its category, among active records.
            builder.HasIndex(x => new { x.FailureCategoryId, x.Title }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_FailureTypes_FailureCategoryId_Title");

            builder.HasOne(x => x.FailureCategory).WithMany(x => x.FailureTypes).HasForeignKey(x => x.FailureCategoryId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}