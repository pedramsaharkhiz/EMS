using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class WidgetConfiguration : IEntityTypeConfiguration<Widget>
    {
        public void Configure(EntityTypeBuilder<Widget> builder)
        {
            builder.ToTable("__PLURAL__");
            builder.HasSoftDeleteFilter();   // required for catalog entities (see SoftDelete)
            builder.HasKey(x => x.WidgetId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
//#if (HasCode)
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);
//#endif
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // Only active records reserve a title.
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX___PLURAL___Title");
        }
    }
}
