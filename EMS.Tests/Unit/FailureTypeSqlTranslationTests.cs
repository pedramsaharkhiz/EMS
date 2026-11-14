using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Dtos.FailureType;
using EMS.Application.Services.FailureType;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using FailureTypeEntity = EMS.Domain.Entities.FailureType;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// The failure-type list (with the equipment types it applies to) and its equipment-type filter must translate to SQL Server; the in-memory provider
    /// used by the API tests would accept anything.
    /// </summary>
    public class FailureTypeSqlTranslationTests
    {
        private sealed class NoUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private sealed class NoAudit : IAuditEventLogger
        {
            public void Created(string entityType, object? entityId) { }
            public void Updated(string entityType, object? entityId) { }
            public void Deleted(string entityType, object? entityId) { }
            public void Operation(string operation, string entityType, object? entityId, string? detail = null) { }
        }

        private static EMSDbContext NewSqlServerContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer("Server=localhost;Database=EmsTranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options,
                new AuditSaveChangesInterceptor(new NoUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));

        [Fact]
        public void The_list_with_its_equipment_types_and_the_equipment_type_filter_translate_to_sql()
        {
            using var context = NewSqlServerContext();
            var repository = new FailureTypeRepository(context);
            var service = new FailureTypeService(repository, new UnitOfWork(context), new NoAudit(), new FailureCategoryRepository(context), new EquipmentTypeRepository(context));

            // the two protected members that make up the list query
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var filters = typeof(FailureTypeService).GetMethod("ApplyFilters", flags)!;
            var listItem = (System.Linq.Expressions.Expression<Func<FailureTypeEntity, FailureTypeListItemDto>>)typeof(FailureTypeService).GetProperty("ListItem", flags | BindingFlags.DeclaredOnly)!.GetValue(service)!;

            var filtered = (IQueryable<FailureTypeEntity>)filters.Invoke(service, new object[] { repository.QueryByActive(true), new FailureTypeListRequestDto { EquipmentTypeId = 7 } })!;
            var sql = filtered.OrderBy(x => x.FailureTypeId).Select(listItem).Skip(0).Take(10).ToQueryString();

            Assert.Contains("[FailureTypeEquipmentTypes]", sql);
            Assert.Contains("EXISTS", sql);                                  // "applies to all" (no link) OR "linked to this type"
            Assert.Contains("[EquipmentTypes]", sql);                        // the titles of the types it applies to
            Assert.DoesNotContain("= 7", sql);                               // the equipment type is a parameter, never concatenated text
        }
    }
}
