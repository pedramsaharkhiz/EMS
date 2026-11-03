using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public class ApplicationUserRoleConfiguration : IEntityTypeConfiguration<ApplicationUserRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationUserRole> builder)
        {
            builder.ToTable("AspNetUserRoles");

            builder.HasKey(x => new { x.UserId, x.RoleId });

            // One active role per user, enforced by the database (inactive rows are history and may repeat).
            builder.HasIndex(x => x.UserId)
                .IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_AspNetUserRoles_UserId_Active");

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.CreatedByUserId)
                .IsRequired(false);

            builder.Property(x => x.CreatedDateTime)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne<ApplicationRole>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
