using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class FailureReportStatusConfiguration : IEntityTypeConfiguration<FailureReportStatus>
    {
        public void Configure(EntityTypeBuilder<FailureReportStatus> builder)
        {
            builder.ToTable("FailureReportStatuses");
            builder.HasSoftDeleteFilter();   // required for catalog entities (see SoftDelete)
            builder.HasKey(x => x.FailureReportStatusId);
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            // IsActive, CreatedByUserId and CreatedDateTime come from BaseEntityConvention.
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // Only active records reserve a title.
            builder.HasData(FailureReportStatusCatalog.Titles.Select(kv => new
            {
                FailureReportStatusId = FailureReportStatusCatalog.IdOf(kv.Key),
                Title = kv.Value,
                Code = kv.Key.ToString(),
                IsActive = true,
                CreatedByUserId = (long?)0,
                CreatedDateTime = SystemPostsGuard.SeedDate,
            }));
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_FailureReportStatuses_Title");
        }
    }
}
