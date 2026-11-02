using EMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EMS.Infrastructure.Persistence.Conventions
{
    /// <summary>
    /// The columns every <see cref="BaseEntity"/> shares are configured here once, for every entity that derives from it
    /// (including entities added in the future), instead of being repeated in each entity configuration:
    /// <list type="bullet">
    /// <item><c>IsActive</c> defaults to true</item>
    /// <item><c>CreatedByUserId</c> is required</item>
    /// <item><c>CreatedDateTime</c> defaults to <c>SYSUTCDATETIME()</c> and is required</item>
    /// </list>
    /// This is a convention, not a rule: an entity configuration that configures one of these columns explicitly
    /// always wins (e.g. master-data entities such as <c>City</c> declare <c>CreatedByUserId</c> optional).
    /// </summary>
    public sealed class BaseEntityConvention : IModelFinalizingConvention
    {
        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
            {
                if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) || entityType.BaseType is not null)
                    continue;

                entityType.FindProperty(nameof(BaseEntity.IsActive))?.Builder
                    .HasDefaultValue(true);

                entityType.FindProperty(nameof(BaseEntity.CreatedByUserId))?.Builder
                    .IsRequired(true);

                entityType.FindProperty(nameof(BaseEntity.CreatedDateTime))?.Builder
                    .HasDefaultValueSql("SYSUTCDATETIME()")?
                    .IsRequired(true);
            }
        }
    }
}
