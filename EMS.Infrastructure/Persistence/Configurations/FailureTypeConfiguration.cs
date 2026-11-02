using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class FailureTypeConfiguration : IEntityTypeConfiguration<FailureType>
    {
        public void Configure(EntityTypeBuilder<FailureType> builder)
        {
            builder.ToTable("FailureTypes");
            builder.HasKey(x => x.FailureTypeId);

            builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(20).IsUnicode(false).IsRequired(false);

            builder.HasOne(x => x.FailureCategory).WithMany(x => x.FailureTypes).HasForeignKey(x => x.FailureCategoryId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}