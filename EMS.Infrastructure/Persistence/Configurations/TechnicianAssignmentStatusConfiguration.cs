using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class TechnicianAssignmentStatusConfiguration : IEntityTypeConfiguration<TechnicianAssignmentStatus>
    {
        public void Configure(EntityTypeBuilder<TechnicianAssignmentStatus> builder)
        {
            builder.ToTable("TechnicianAssignmentStatuses");

            builder.HasKey(x => x.TechnicianAssignmentStatusId);

            builder.Property(x => x.Title)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
        }
    }
}
