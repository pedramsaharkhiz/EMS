using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class SupportingDepartmentConfiguration : IEntityTypeConfiguration<SupportingDepartment>
    {
        public void Configure(EntityTypeBuilder<SupportingDepartment> builder)
        {
            builder.ToTable("SupportingDepartments");
            builder.HasKey(x => x.SupportingDepartmentId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();
            builder.HasIndex(x => x.Title).IsUnique().HasDatabaseName("UX_SupportingDepartments_Title");
            builder.HasOne(x => x.City).WithMany(x => x.SupportingDepartments).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}