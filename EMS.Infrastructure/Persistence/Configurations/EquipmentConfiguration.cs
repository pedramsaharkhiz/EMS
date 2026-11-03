using EMS.Domain;
using EMS.Domain.Entities;
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
            // Only active equipment reserves its Yaas code (a deactivated one frees it, like a catalog title).
            builder.HasIndex(x => x.YaasCode).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_Equipments_YaasCode");

            builder.HasOne(x => x.EquipmentType).WithMany(x => x.Equipments).HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.EquipmentModel).WithMany(x => x.Equipments).HasForeignKey(x => x.EquipmentModelId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.EquipmentStatus).WithMany(x => x.Equipments).HasForeignKey(x => x.EquipmentStatusId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.CurrentEquipmentLocationType).WithMany(x => x.CurrentEquipments).HasForeignKey(x => x.CurrentEquipmentLocationTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.CurrentOrganization).WithMany(x => x.CurrentEquipments).HasForeignKey(x => x.CurrentOrganizationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.CurrentSupportingDepartment).WithMany(x => x.CurrentEquipments).HasForeignKey(x => x.CurrentSupportingDepartmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.NoAction);

            builder.ToTable(t => t.HasCheckConstraint("CK_Equipments_TypeOrModel_ExactlyOne", "([EquipmentTypeId] IS NOT NULL AND [EquipmentModelId] IS NULL) OR ([EquipmentTypeId] IS NULL AND [EquipmentModelId] IS NOT NULL)"));
        }
    }
}