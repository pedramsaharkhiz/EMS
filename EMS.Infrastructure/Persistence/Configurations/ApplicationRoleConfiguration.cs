using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.ToTable("AspNetRoles");

            builder.HasKey(x => x.Id);

            // Role names are typed by users and may be Persian: nvarchar (not varchar).
            builder.Property(x => x.Name)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(x => x.NormalizedName)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(x => x.ConcurrencyStamp)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.CreatedByUserId)
                .IsRequired();

            builder.Property(x => x.CreatedDateTime)
                .HasDefaultValueSql("SYSUTCDATETIME()")
                .IsRequired();
        }
    }
}
