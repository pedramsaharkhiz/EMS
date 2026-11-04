using EMS.Infrastructure.Persistence.Identity;
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
            builder.HasSoftDeleteFilter();
            builder.HasKey(x => x.FailureReportId);

            builder.Property(x => x.EmployeeId).IsRequired(false);
            builder.Property(x => x.ReferenceNumber).HasMaxLength(50).IsRequired(false);
            builder.Property(x => x.PriorityId).IsRequired();
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // The reference number is optional; when given, only an active report reserves it.
            builder.HasIndex(x => x.ReferenceNumber).IsUnique().HasFilter("[IsActive] = 1 AND [ReferenceNumber] IS NOT NULL").HasDatabaseName("UX_FailureReports_ReferenceNumber");
            builder.HasIndex(x => x.ReportedDateTime).HasDatabaseName("IX_FailureReports_ReportedDateTime");
            builder.Property(x => x.ReportedDateTime).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);

            builder.HasOne(x => x.Equipment).WithMany(x => x.FailureReports).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.FailureType).WithMany(x => x.FailureReports).HasForeignKey(x => x.FailureTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.ReportType).WithMany(x => x.FailureReports).HasForeignKey(x => x.ReportTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.FailureReportStatus).WithMany(x => x.FailureReports).HasForeignKey(x => x.FailureReportStatusId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReportedByUserId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Organization).WithMany(x => x.FailureReports).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Employee).WithMany(x => x.FailureReports).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}