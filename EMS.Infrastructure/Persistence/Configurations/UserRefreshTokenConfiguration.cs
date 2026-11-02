using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
    {
        public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
        {
            builder.ToTable("UserRefreshTokens");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash).HasMaxLength(128).IsUnicode(false).IsRequired();
            builder.Property(x => x.ExpiresDateTime).IsRequired();
            builder.Property(x => x.RevokedDateTime).IsRequired(false);
            builder.Property(x => x.CreatedByIp).HasMaxLength(45).IsUnicode(false).IsRequired();
            builder.Property(x => x.RevokedByIp).HasMaxLength(45).IsUnicode(false).IsRequired(false);

            builder.Ignore(x => x.IsRevoked);
            builder.Ignore(x => x.IsExpired);
            builder.Ignore(x => x.IsValid);

            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired();
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}