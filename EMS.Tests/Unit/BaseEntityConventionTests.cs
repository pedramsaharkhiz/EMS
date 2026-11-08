using EMS.Domain.Entities;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EMS.Tests.Unit
{
    /// <summary>Every BaseEntity gets the shared columns from the convention, including entities added later.</summary>
    public class BaseEntityConventionTests
    {
        /// <summary>
        /// Entities whose configuration explicitly makes CreatedByUserId optional (master data / link rows that can be
        /// created without a signed-in user). Any other BaseEntity must follow the convention: required.
        /// </summary>
        private static readonly HashSet<string> CreatedByNullableExceptions = new()
        {
            nameof(City), nameof(Province), nameof(Employee), nameof(Field), nameof(Skill), nameof(Zone),
            nameof(Organization), nameof(OrganizationManagement), nameof(OrganizationStructure),
            nameof(TechnicianSkill)
        };

        private static List<IEntityType> BaseEntities()
        {
            using var db = TestDb.SqlServerModelOnly();
            return db.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model
                .GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
                .ToList();
        }

        [Fact]
        public void Every_base_entity_has_the_shared_columns_configured()
        {
            var entities = BaseEntities();
            Assert.True(entities.Count >= 30);

            foreach (var entity in entities)
            {
                var name = entity.ClrType.Name;

                var isActive = entity.FindProperty(nameof(BaseEntity.IsActive))!;
                Assert.True(isActive.GetDefaultValue() is true, $"{name}.IsActive must default to true");
                Assert.False(isActive.IsNullable, $"{name}.IsActive must not be nullable");

                var createdDate = entity.FindProperty(nameof(BaseEntity.CreatedDateTime))!;
                Assert.Equal("SYSUTCDATETIME()", createdDate.GetDefaultValueSql());
                Assert.False(createdDate.IsNullable, $"{name}.CreatedDateTime must not be nullable");

                var createdBy = entity.FindProperty(nameof(BaseEntity.CreatedByUserId))!;
                Assert.True(CreatedByNullableExceptions.Contains(name) == createdBy.IsNullable, $"{name}.CreatedByUserId nullable = {createdBy.IsNullable}");
            }
        }

        [Fact]
        public void An_explicit_entity_configuration_wins_over_the_convention()
        {
            var organization = BaseEntities().Single(t => t.ClrType == typeof(Organization));
            Assert.True(organization.FindProperty(nameof(BaseEntity.CreatedByUserId))!.IsNullable);
        }
    }
}
