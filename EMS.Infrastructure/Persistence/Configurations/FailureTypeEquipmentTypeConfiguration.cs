using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    /// <summary>The equipment types a failure type applies to: one row per (failure type, equipment type).</summary>
    public sealed class FailureTypeEquipmentTypeConfiguration : IEntityTypeConfiguration<FailureTypeEquipmentType>
    {
        public void Configure(EntityTypeBuilder<FailureTypeEquipmentType> builder)
        {
            builder.ToTable("FailureTypeEquipmentTypes");
            builder.HasKey(x => new { x.FailureTypeId, x.EquipmentTypeId });
            builder.HasOne<FailureType>().WithMany(x => x.EquipmentTypes).HasForeignKey(x => x.FailureTypeId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.EquipmentType).WithMany().HasForeignKey(x => x.EquipmentTypeId).OnDelete(DeleteBehavior.NoAction);
            // "which failure types apply to this equipment type" (the dropdown filter)
            builder.HasIndex(x => x.EquipmentTypeId).HasDatabaseName("IX_FailureTypeEquipmentTypes_EquipmentTypeId");
        }
    }
}
