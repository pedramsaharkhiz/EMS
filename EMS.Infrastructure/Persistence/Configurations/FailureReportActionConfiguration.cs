using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class FailureReportActionConfiguration : IEntityTypeConfiguration<FailureReportAction>
    {
        public void Configure(EntityTypeBuilder<FailureReportAction> builder)
        {
            builder.ToTable("FailureReportActions");
            builder.HasKey(x => x.FailureReportActionId);
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.HasOne(x => x.FailureReport).WithMany(x => x.FailureReportActions).HasForeignKey(x => x.FailureReportId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Action).WithMany(x => x.FailureReportActions).HasForeignKey(x => x.ActionId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}