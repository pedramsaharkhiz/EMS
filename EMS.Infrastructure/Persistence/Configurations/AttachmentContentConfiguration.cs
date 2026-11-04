using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class AttachmentContentConfiguration : IEntityTypeConfiguration<AttachmentContent>
    {
        public void Configure(EntityTypeBuilder<AttachmentContent> builder)
        {
            builder.ToTable("AttachmentContents");
            builder.HasKey(x => x.StreamId);
            builder.Property(x => x.StreamId).ValueGeneratedNever();

            builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(100).IsUnicode(false).IsRequired();
            builder.Property(x => x.SizeBytes).IsRequired();
            builder.Property(x => x.Sha256).HasColumnType("varbinary(32)").IsRequired(false);

            // The file itself: a shadow column, so no code can load the bytes by accident (see AttachmentContent).
            builder.Property<byte[]>("Content").HasColumnType("varbinary(max)").IsRequired(false);
        }
    }
}
