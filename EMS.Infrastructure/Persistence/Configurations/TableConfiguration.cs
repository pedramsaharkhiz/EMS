using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class TableConfiguration : IEntityTypeConfiguration<Table>
    {
        public void Configure(EntityTypeBuilder<Table> builder)
        {
            builder.ToTable("Tables");

            builder.HasKey(x => x.TableId);

            builder.Property(x => x.TableName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .IsRequired();

            // Shown to users, may be Persian: nvarchar.
            builder.Property(x => x.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.IsAttachmentEnabled)
                .IsRequired();

        }
    }
}
