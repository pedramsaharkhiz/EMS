using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
    {
        public void Configure(EntityTypeBuilder<Equipment> builder)
        {
            builder.ToTable("Equipments");
            builder.HasSoftDeleteFilter();   // deactivated equipment is invisible unless a query asks for it (see SoftDelete)
            builder.HasKey(x => x.EquipmentId);

            builder.Property(x => x.SerialNumber).HasMaxLength(EquipmentConstraints.SerialNumberMaxLength).IsRequired(false);
            builder.Property(x => x.AssetNumber).HasMaxLength(EquipmentConstraints.AssetNumberMaxLength).IsRequired(false);
            builder.Property(x => x.YaasCode).HasMaxLength(EquipmentConstraints.YaasCodeMaxLength).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(EquipmentConstraints.DescriptionMaxLength).IsRequired(false);
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // The Yaas code is NOT unique (several equipment may share one). The serial number and the asset number are, among ALL equipment (a
            // deactivated one keeps its numbers: it is the same physical device); equipment without one does not collide with another.
            builder.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("[SerialNumber] IS NOT NULL").HasDatabaseName(EquipmentConstraints.SerialNumberIndexName);
            builder.HasIndex(x => x.AssetNumber).IsUnique().HasFilter("[AssetNumber] IS NOT NULL").HasDatabaseName(EquipmentConstraints.AssetNumberIndexName);

            builder.HasOne(x => x.EquipmentType).WithMany(x => x.Equipments).HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.EquipmentModel).WithMany(x => x.Equipments).HasForeignKey(x => x.EquipmentModelId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.EquipmentStatus).WithMany(x => x.Equipments).HasForeignKey(x => x.EquipmentStatusId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.CurrentOrganization).WithMany(x => x.CurrentEquipments).HasForeignKey(x => x.CurrentOrganizationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.CurrentSupportingDepartment).WithMany(x => x.CurrentEquipments).HasForeignKey(x => x.CurrentSupportingDepartmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.NoAction);

            // The owner must be one of the defined values (the API refuses anything else; this keeps the table honest too).
            builder.ToTable(t => t.HasCheckConstraint(EquipmentConstraints.OwnerCheckName, $"[Owner] IN ({string.Join(", ", Enum.GetValues<EquipmentOwner>().Select(owner => (int)owner))})"));
            // With a branch or with a supporting department: exactly one of the two columns is filled.
            builder.ToTable(t => t.HasCheckConstraint(EquipmentConstraints.LocationCheckName,
                "([CurrentOrganizationId] IS NOT NULL AND [CurrentSupportingDepartmentId] IS NULL) OR ([CurrentOrganizationId] IS NULL AND [CurrentSupportingDepartmentId] IS NOT NULL)"));
            builder.ToTable(t => t.HasCheckConstraint(EquipmentConstraints.TechnicianCheckName,
                "[TechnicianUserId] IS NULL OR [CurrentSupportingDepartmentId] IS NOT NULL"));
            builder.ToTable(t => t.HasCheckConstraint("CK_Equipments_TypeOrModel_ExactlyOne", "([EquipmentTypeId] IS NOT NULL AND [EquipmentModelId] IS NULL) OR ([EquipmentTypeId] IS NULL AND [EquipmentModelId] IS NOT NULL)"));
        }
    }
}