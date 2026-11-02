using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TableName).HasMaxLength(128).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Action).HasMaxLength(16).IsUnicode(false).IsRequired();
            builder.Property(x => x.OldValues);
            builder.Property(x => x.NewValues);
            builder.Property(x => x.ChangedColumns);
            builder.Property(x => x.UserName).HasMaxLength(256);
            builder.Property(x => x.IpAddress).HasMaxLength(45).IsUnicode(false);
            builder.Property(x => x.CorrelationId).HasMaxLength(64).IsUnicode(false);
            builder.Property(x => x.TimestampUtc).IsRequired();

            builder.HasIndex(x => new { x.TableName, x.EntityId });
            builder.HasIndex(x => x.TimestampUtc);
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.CorrelationId);
        }
    }
}
