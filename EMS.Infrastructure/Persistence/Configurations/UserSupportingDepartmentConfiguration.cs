using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    /// <summary>The departments a user works for: one row per (user, department). The user side is configured on <c>ApplicationUser</c>.</summary>
    public sealed class UserSupportingDepartmentConfiguration : IEntityTypeConfiguration<UserSupportingDepartment>
    {
        public void Configure(EntityTypeBuilder<UserSupportingDepartment> builder)
        {
            builder.ToTable("UserSupportingDepartments");
            builder.HasKey(x => new { x.UserId, x.SupportingDepartmentId });
            builder.HasOne<SupportingDepartment>().WithMany().HasForeignKey(x => x.SupportingDepartmentId).OnDelete(DeleteBehavior.NoAction);
            // "which users work for this department" (the department filter of the users list, the row policies)
            builder.HasIndex(x => x.SupportingDepartmentId).HasDatabaseName("IX_UserSupportingDepartments_SupportingDepartmentId");
        }
    }
}
