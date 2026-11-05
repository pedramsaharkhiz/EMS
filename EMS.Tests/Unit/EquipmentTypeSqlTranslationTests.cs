using EMS.Application.Common.Interfaces;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit
{
    /// <summary>The two equipment-type lookups must translate to SQL Server (the in-memory provider used by the API tests would accept anything).</summary>
    public class EquipmentTypeSqlTranslationTests
    {
        private sealed class NoUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private static EMSDbContext NewSqlServerContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer("Server=localhost;Database=EmsTranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options,
                new AuditSaveChangesInterceptor(new NoUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));

        [Fact]
        public void The_types_without_models_query_translates_and_asks_for_no_active_model()
        {
            using var context = NewSqlServerContext();
            var sql = new EquipmentTypeRepository(context).QueryWithoutModels()
                .Where(x => x.Title.Contains("a") && x.EquipmentCategoryId == 3).OrderBy(x => x.EquipmentCategoryTitle).Skip(10).Take(10).ToQueryString();

            Assert.Contains("[EquipmentTypes]", sql);
            Assert.Contains("NOT EXISTS", sql);
            Assert.Contains("[EquipmentModels]", sql);
            Assert.Contains("[IsActive] = CAST(1 AS bit)", sql);
        }

        [Fact]
        public void The_type_of_a_model_query_translates_and_lifts_the_soft_delete_filter()
        {
            using var context = NewSqlServerContext();
            var sql = new EquipmentTypeRepository(context).QueryTypeOfModel(5).ToQueryString();

            Assert.Contains("[EquipmentModels]", sql);
            Assert.Contains("[EquipmentTypes]", sql);
            Assert.Contains("[EquipmentCategories]", sql);
        }
    }
}
