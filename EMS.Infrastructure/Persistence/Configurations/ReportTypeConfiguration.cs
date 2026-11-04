using EMS.Domain;
using EMS.Infrastructure.Persistence.Seed;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class ReportTypeConfiguration : IEntityTypeConfiguration<ReportType>
    {
        public void Configure(EntityTypeBuilder<ReportType> builder)
        {
            builder.ToTable("ReportTypes");

            builder.HasKey(x => x.ReportTypeId);

            builder.HasSoftDeleteFilter();   // required for catalog entities (see SoftDelete)
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_ReportTypes_Title");
            builder.HasData(ReportTypeCatalog.Titles.Select(kv => new
            {
                ReportTypeId = ReportTypeCatalog.IdOf(kv.Key),
                Title = kv.Value,
                Code = kv.Key.ToString(),
                IsActive = true,
                CreatedByUserId = (long?)0,
                CreatedDateTime = SystemPostsGuard.SeedDate,
            }));



        }
    }
}
