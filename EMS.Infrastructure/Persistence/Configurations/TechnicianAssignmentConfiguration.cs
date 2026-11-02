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

            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.AssignedDateTime).IsRequired();
            builder.Property(x => x.DeadlineDateTime).IsRequired(false);
            builder.Property(x => x.CompletedDateTime).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

            builder.HasOne(x => x.FailureReport).WithMany(x => x.TechnicianAssignments).HasForeignKey(x => x.FailureReportId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.TechnicianAssignmentStatus).WithMany(x => x.TechnicianAssignments).HasForeignKey(x => x.TechnicianAssignmentStatusId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}