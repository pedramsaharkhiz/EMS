using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EntityAttachmentConfiguration : IEntityTypeConfiguration<EntityAttachment>
    {
        public void Configure(EntityTypeBuilder<EntityAttachment> builder)
        {
            builder.ToTable("EntityAttachments");
            builder.HasKey(x => x.EntityAttachmentId);

            builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.StreamId).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.HasOne(x => x.Table).WithMany(x => x.EntityAttachments).HasForeignKey(x => x.TableId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}