using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class EquipmentStatusConfiguration : IEntityTypeConfiguration<EquipmentStatus>
    {
        public void Configure(EntityTypeBuilder<EquipmentStatus> builder)
        {
            builder.ToTable("EquipmentStatuses");
            builder.HasSoftDeleteFilter();
            builder.HasKey(x => x.EquipmentStatusId);
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_EquipmentStatuses_Title");

            // The system statuses (EquipmentStatusCode): id = enum value, Code = enum name. Inserted once by the migration;
            // later edits of the titles through the API are kept. Do not change these values in a later migration unless you
            // mean to overwrite what administrators edited. Dates are fixed because seed data must be deterministic.
            builder.HasData(EquipmentStatusCatalog.Titles.Select(kv => new
            {
                EquipmentStatusId = EquipmentStatusCatalog.IdOf(kv.Key),
                Title = kv.Value,
                Code = kv.Key.ToString(),
                IsActive = true,
                CreatedByUserId = (long?)0,
                CreatedDateTime = new DateTime(2026, 11, 3, 0, 0, 0, DateTimeKind.Utc),
            }));
        }
    }
}