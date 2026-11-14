using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("AspNetUsers");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserName).HasMaxLength(20).IsRequired();
            builder.Property(x => x.NormalizedUserName).HasMaxLength(20).IsRequired(false);
            builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.PersonnelCode).HasMaxLength(32).IsUnicode(false).IsRequired();
            builder.Property(x => x.NationalCode).HasMaxLength(10).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.Email).HasMaxLength(256).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.NormalizedEmail).HasMaxLength(256).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.EmailConfirmed).IsRequired();
            builder.Property(x => x.PasswordHash).HasMaxLength(512).IsUnicode(false).IsRequired();
            builder.Property(x => x.SecurityStamp).HasMaxLength(256).IsRequired(false);
            builder.Property(x => x.ConcurrencyStamp).HasMaxLength(256).IsRequired(false);
            builder.Property(x => x.PhoneNumberConfirmed).IsRequired();
            builder.Property(x => x.TwoFactorEnabled).IsRequired();
            builder.Property(x => x.LockoutEnd).HasColumnType("datetimeoffset(7)").IsRequired(false);
            builder.Property(x => x.LockoutEnabled).IsRequired();
            builder.Property(x => x.AccessFailedCount).IsRequired();
            builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
            builder.Property(x => x.CreatedByUserId).IsRequired(false);
            builder.Property(x => x.CreatedDateTime).HasDefaultValueSql("SYSUTCDATETIME()").IsRequired();

            builder.HasMany(x => x.SupportingDepartments).WithOne().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            builder.Property(x => x.PostId).IsRequired(false);
            builder.HasOne(x => x.Post).WithMany().HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}