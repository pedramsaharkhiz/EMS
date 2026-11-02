using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class OrganizationStructureConfiguration : IEntityTypeConfiguration<OrganizationStructure>
    {
        public void Configure(EntityTypeBuilder<OrganizationStructure> builder)
        {
            builder.ToTable("OrganizationStructures");
            builder.HasKey(x => x.OrganizationStructureId);
            builder.Property(x => x.OrganizationStructureCode).HasMaxLength(20).IsRequired(false);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired(false);
            builder.Property(x => x.Grade).IsRequired(false);
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasOne(x => x.ParentOrganizationStructure).WithMany(x => x.Children).HasForeignKey(x => x.ParentOrganizationStructureId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}