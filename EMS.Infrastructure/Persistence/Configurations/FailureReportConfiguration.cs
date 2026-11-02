using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class FailureReportConfiguration : IEntityTypeConfiguration<FailureReport>
    {
        public void Configure(EntityTypeBuilder<FailureReport> builder)
        {
            builder.ToTable("FailureReports");
            builder.HasKey(x => x.FailureReportId);

            builder.Property(x => x.EmployeeId).IsRequired(false);
            builder.Property(x => x.ReferenceNumber).HasMaxLength(50).IsRequired(false);
            builder.Property(x => x.PriorityId).IsRequired();
            builder.Property(x => x.ReportedDateTime).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

            builder.HasOne(x => x.Equipment).WithMany(x => x.FailureReports).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.FailureType).WithMany(x => x.FailureReports).HasForeignKey(x => x.FailureTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.ReportType).WithMany(x => x.FailureReports).HasForeignKey(x => x.ReportTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Organization).WithMany(x => x.FailureReports).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Employee).WithMany(x => x.FailureReports).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}