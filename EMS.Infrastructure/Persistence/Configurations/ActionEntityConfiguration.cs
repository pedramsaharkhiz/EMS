using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class ActionEntityConfiguration : IEntityTypeConfiguration<ActionEntity>
    {
        public void Configure(EntityTypeBuilder<ActionEntity> builder)
        {
            builder.ToTable("ActionEntities");

            builder.HasKey(x => x.ActionId);

            builder.HasSoftDeleteFilter();   // required for catalog entities (see SoftDelete)
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_ActionEntities_Title");

            builder.Property(x => x.Description)
                .HasMaxLength(500)
                .IsRequired(false);

        }
    }
}
