using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations;

public sealed class AssignmentNoteConfiguration : IEntityTypeConfiguration<AssignmentNote>
{
    public void Configure(EntityTypeBuilder<AssignmentNote> builder)
    {
        builder.ToTable("AssignmentNotes");
        builder.HasKey(x => x.AssignmentNoteId);
        builder.HasSoftDeleteFilter();
        builder.Property(x => x.Text).HasMaxLength(AssignmentNoteConstraints.TextMaxLength).IsRequired();
        builder.Property(x => x.SeenDateTime).IsRequired(false);
        builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
        builder.HasOne(x => x.TechnicianAssignment).WithMany(x => x.AssignmentNotes)
            .HasForeignKey(x => x.TechnicianAssignmentId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(x => x.NoteCategory).WithMany().HasForeignKey(x => x.NoteCategoryId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.TechnicianAssignmentId, x.CreatedDateTime, x.AssignmentNoteId });
    }
}
