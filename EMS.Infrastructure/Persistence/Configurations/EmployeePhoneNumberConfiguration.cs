using EMS.Domain;
using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations;

public sealed class EmployeePhoneNumberConfiguration : IEntityTypeConfiguration<EmployeePhoneNumber>
{
    public void Configure(EntityTypeBuilder<EmployeePhoneNumber> builder)
    {
        builder.ToTable("EmployeePhoneNumbers", t => t.HasCheckConstraint("CK_EmployeePhoneNumbers_Mobile",
            "LEN([PhoneNumber]) = 11 AND [PhoneNumber] LIKE '09%' AND [PhoneNumber] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'"));
        builder.HasKey(x => x.EmployeePhoneNumberId);
        builder.HasSoftDeleteFilter();
        builder.Property(x => x.PhoneNumber).HasMaxLength(11).IsUnicode(false).IsRequired();
        builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
        builder.HasIndex(x => x.PhoneNumber).IsUnique().HasDatabaseName(EmployeePhoneRules.UniqueIndex);
        builder.HasOne(x => x.Employee).WithMany(x => x.PhoneNumbers).HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
    }
}
