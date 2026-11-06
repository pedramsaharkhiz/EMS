using EMS.Domain;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("Organizations");
            // Like the other master-data entities (City, Province, Zone, ...), Organizations can be created without a signed-in
            // user, so CreatedByUserId stays nullable. (Without this line the BaseEntityConvention would make it required.)
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.HasKey(x => x.OrganizationId);

            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.Address).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.Latitude).HasPrecision(9, 6).IsRequired(false);
            builder.Property(x => x.Longitude).HasPrecision(9, 6).IsRequired(false);

            // Exactly one parent: a field or a branch management (never neither, never both).
            builder.ToTable(t => t.HasCheckConstraint(OrganizationConstraints.ParentCheckName, OrganizationConstraints.ParentCheckSql));

            builder.HasOne(x => x.Field).WithMany(x => x.Organizations).HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.OrganizationManagement).WithMany(x => x.Organizations).HasForeignKey(x => x.OrganizationManagementId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.SupportingDepartment).WithMany(x => x.Organizations).HasForeignKey(x => x.SupportingDepartmentId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.OrganizationType).WithMany(x => x.Organizations).HasForeignKey(x => x.OrganizationTypeId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}