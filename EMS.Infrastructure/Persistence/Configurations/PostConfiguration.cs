using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMS.Infrastructure.Persistence.Configurations
{
    public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.ToTable("Posts");
            builder.HasSoftDeleteFilter();   // required for catalog entities (see SoftDelete)
            builder.HasKey(x => x.PostId);
            builder.Property(x => x.Title).HasMaxLength(CatalogConstraints.TitleMaxLength).IsRequired();
            builder.Property(x => x.Code).HasMaxLength(CatalogConstraints.CodeMaxLength).IsUnicode(false).IsRequired(false);
            // IsActive, CreatedByUserId and CreatedDateTime come from BaseEntityConvention.
            // Optimistic concurrency without a new column: an update fails if the row was modified by someone else after it was read.
            builder.Property(x => x.ModifiedDateTime).IsConcurrencyToken();
            // Only active records reserve a title.
            builder.HasIndex(x => x.Title).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_Posts_Title");

            // The system posts (PostCode): id = enum value, Code = enum name. Inserted by the migration (and re-created at start-up by
            // SystemPostsGuard if they are ever missing); later edits of the titles through the API are kept. Dates are fixed because
            // seed data must be deterministic.
            builder.HasData(PostCatalog.Titles.Select(kv => new
            {
                PostId = PostCatalog.IdOf(kv.Key),
                Title = kv.Value,
                Code = kv.Key.ToString(),
                IsActive = true,
                CreatedByUserId = (long?)0,
                CreatedDateTime = SystemPostsGuard.SeedDate,
            }));
        }
    }
}
