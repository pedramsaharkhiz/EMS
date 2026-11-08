using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class TechnicianAssignmentStatusConfiguration : IEntityTypeConfiguration<TechnicianAssignmentStatus>
    {
        public void Configure(EntityTypeBuilder<TechnicianAssignmentStatus> builder)
        {
            builder.ToTable("TechnicianAssignmentStatuses");
            builder.HasKey(x => x.TechnicianAssignmentStatusId);

            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired();

            builder.HasData(TechnicianAssignmentCatalog.Titles.Select(kv => new
            {
                TechnicianAssignmentStatusId = TechnicianAssignmentCatalog.IdOf(kv.Key),
                Title = kv.Value,
                Code = kv.Key.ToString(),
                IsActive = true,
                CreatedByUserId = (long?)0,
                CreatedDateTime = SystemPostsGuard.SeedDate,
            }));
            builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_TechnicianAssignmentStatuses_Code");
        }
    }
}
