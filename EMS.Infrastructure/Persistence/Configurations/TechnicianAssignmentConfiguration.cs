using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public class TechnicianAssignmentConfiguration : IEntityTypeConfiguration<TechnicianAssignment>
    {
        public void Configure(EntityTypeBuilder<TechnicianAssignment> builder)
        {
            builder.ToTable("TechnicianAssignments");
            builder.HasKey(x => x.TechnicianAssignmentId);

            builder.Property(x => x.Description).HasMaxLength(TechnicianAssignmentConstraints.DescriptionMaxLength).IsRequired(false);
            builder.Property(x => x.ResultNote).HasMaxLength(TechnicianAssignmentConstraints.NoteMaxLength).IsRequired(false);
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.Property(x => x.AssignedDateTime).IsRequired();
            builder.Property(x => x.DeadlineDateTime).IsRequired(false);
            builder.Property(x => x.CompletedDateTime).IsRequired(false);

            // A report may have several open assignments (assigned, accepted or in progress), but a technician at most one per report;
            // finished ones stay as history.
            builder.HasIndex(x => new { x.FailureReportId, x.TechnicianUserId })
                .IsUnique()
                .HasFilter($"[TechnicianAssignmentStatusId] IN ({string.Join(", ", TechnicianAssignmentCatalog.OpenIds)})")
                .HasDatabaseName(TechnicianAssignmentConstraints.OneOpenPerReportAndTechnicianIndexName);
            builder.HasIndex(x => new { x.TechnicianUserId, x.TechnicianAssignmentStatusId }).HasDatabaseName("IX_TechnicianAssignments_Technician_Status");

            builder.HasOne(x => x.FailureReport).WithMany(x => x.TechnicianAssignments).HasForeignKey(x => x.FailureReportId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.TechnicianAssignmentStatus).WithMany(x => x.TechnicianAssignments).HasForeignKey(x => x.TechnicianAssignmentStatusId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}