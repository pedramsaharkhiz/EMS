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
            builder.HasSoftDeleteFilter();
            builder.HasKey(x => x.SupportingDepartmentId);
            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_SupportingDepartments_Title");
            builder.HasOne(x => x.City).WithMany(x => x.SupportingDepartments).HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}