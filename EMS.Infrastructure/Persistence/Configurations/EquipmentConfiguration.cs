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
            builder.HasKey(x => x.EquipmentId);

            builder.Property(x => x.SerialNumber).HasMaxLength(50).IsRequired(false);
            builder.Property(x => x.AssetNumber).HasMaxLength(50).IsRequired(false);
            builder.Property(x => x.YaasCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

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