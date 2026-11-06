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
            // Concurrency token: a refresh token can be consumed by exactly one request (UPDATE ... WHERE RevokedDateTime IS NULL).
            builder.Property(x => x.RevokedDateTime).IsRequired(false).IsConcurrencyToken();
            builder.Property(x => x.CreatedByIp).HasMaxLength(45).IsUnicode(false).IsRequired();
            builder.Property(x => x.RevokedByIp).HasMaxLength(45).IsUnicode(false).IsRequired(false);

            // Refresh and logout look a token up by its hash (unique: a SHA-256 of a random value); the clean-up job finds expired / revoked ones.
            builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("UX_UserRefreshTokens_TokenHash");
            builder.HasIndex(x => x.ExpiresDateTime).HasDatabaseName("IX_UserRefreshTokens_ExpiresDateTime");
            builder.HasIndex(x => x.RevokedDateTime).HasFilter("[RevokedDateTime] IS NOT NULL").HasDatabaseName("IX_UserRefreshTokens_RevokedDateTime");

            builder.Ignore(x => x.IsRevoked);
            builder.Ignore(x => x.IsExpired);
            builder.Ignore(x => x.IsValid);


            builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}