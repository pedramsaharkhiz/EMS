using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentLocationHistoryConfiguration : IEntityTypeConfiguration<EquipmentLocationHistory>
    {
        public void Configure(EntityTypeBuilder<EquipmentLocationHistory> builder)
        {
            builder.ToTable("EquipmentLocationHistories");
            builder.HasKey(x => x.EquipmentLocationHistoryId);

            builder.Property(x => x.AssignedDateTime).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);

            builder.ToTable(t => t.HasCheckConstraint(EquipmentConstraints.HistoryLocationCheckName,
                "([OrganizationId] IS NOT NULL AND [SupportingDepartmentId] IS NULL) OR ([OrganizationId] IS NULL AND [SupportingDepartmentId] IS NOT NULL)"));

            builder.HasOne(x => x.Equipment).WithMany(x => x.EquipmentLocationHistories).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Organization).WithMany(x => x.EquipmentLocationHistories).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.SupportingDepartment).WithMany(x => x.EquipmentLocationHistories).HasForeignKey(x => x.SupportingDepartmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}