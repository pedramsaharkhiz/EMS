using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class TechnicianAssignmentActionConfiguration : IEntityTypeConfiguration<TechnicianAssignmentAction>
    {
        public void Configure(EntityTypeBuilder<TechnicianAssignmentAction> builder)
        {
            builder.ToTable("TechnicianAssignmentActions");
            builder.HasKey(x => x.TechnicianAssignmentActionId);

            builder.Property(x => x.ActionDateTime).IsRequired();
            builder.HasIndex(x => x.TechnicianAssignmentId).HasDatabaseName("IX_TechnicianAssignmentActions_Assignment");
            builder.Property(x => x.Description).HasMaxLength(EMS.Domain.TechnicianAssignmentConstraints.NoteMaxLength).IsRequired(false);

            builder.HasOne(x => x.TechnicianAssignment).WithMany(x => x.TechnicianAssignmentActions).HasForeignKey(x => x.TechnicianAssignmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.Action).WithMany(x => x.TechnicianAssignmentActions).HasForeignKey(x => x.ActionId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.EquipmentLocationHistory).WithMany(x => x.TechnicianAssignmentActions).HasForeignKey(x => x.EquipmentLocationHistoryId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}