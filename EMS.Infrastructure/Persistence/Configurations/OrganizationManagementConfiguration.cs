using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class OrganizationManagementConfiguration : IEntityTypeConfiguration<OrganizationManagement>
    {
        public void Configure(EntityTypeBuilder<OrganizationManagement> builder)
        {
            builder.ToTable("OrganizationManagements");
            builder.HasKey(x => x.OrganizationManagementId);

            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

            builder.HasOne(x => x.Zone).WithMany(x => x.OrganizationManagements).HasForeignKey(x => x.ZoneId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}