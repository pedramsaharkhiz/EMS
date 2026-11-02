using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");
            builder.HasKey(x => x.EmployeeId);
            builder.Property(x => x.PersonnelCode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
            builder.Property(x => x.BirthDate).HasColumnType("datetime2").IsRequired();
            builder.Property(x => x.ProfilePictureUrl).HasMaxLength(255).IsRequired(false);
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.HasOne(x => x.Organization).WithMany(x => x.Employees).HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.NoAction);
            builder.HasOne(x => x.OrganizationStructure).WithMany(x => x.Employees).HasForeignKey(x => x.OrganizationStructureId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}