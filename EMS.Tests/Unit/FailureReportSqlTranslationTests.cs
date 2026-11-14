using EMS.Application.Common.Export;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Dtos.FailureReport;
using EMS.Application.Services.FailureReport;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// The integration tests run on the in-memory provider, which cannot reveal a query SQL Server cannot translate. This builds the real
    /// SQL Server provider (without connecting) and asks EF to translate the failure report list/export query with every filter and each
    /// sort, including the reporter's person set, so an untranslatable construct fails here instead of in production.
    /// </summary>
    public class FailureReportSqlTranslationTests
    {
        private sealed class CapturingExporter : IExcelExporter
        {
            public IQueryable? Rows { get; private set; }

            public Task<ExcelFile> ExportAsync<TRow>(IQueryable<TRow> rows, ExcelExportDefinition<TRow> definition, CancellationToken cancellationToken = default)
            {
                Rows = rows;
                return Task.FromResult(new ExcelFile(Array.Empty<byte>(), "x.xlsx"));
            }
        }

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

        private static (FailureReportService Service, CapturingExporter Exporter, EMSDbContext Context) NewService()
        {
            var context = NewSqlServerContext();
            var exporter = new CapturingExporter();
            var service = new FailureReportService(
                new FailureReportRepository(context), new EquipmentRepository(context), new FailureTypeRepository(context), new ReportTypeRepository(context),
                new FailureReportStatusRepository(context), new Repository<EMS.Domain.Entities.Organization>(context), new Repository<EMS.Domain.Entities.Employee>(context),
                new NoUser(), new UnitOfWork(context), new NoAudit(), exporter, new EMS.Tests.Support.PassThroughRowAccess(), new UserReferenceRepository(context));
            return (service, exporter, context);
        }

        private static FailureReportListRequestDto EveryFilter() => new()
        {
            Search = "ab",
            ReferenceNumber = "r",
            EquipmentIds = new long[] { 1, 2 },
            FailureTypeIds = new long[] { 3 },
            FailureCategoryIds = new long[] { 4 },
            ReportTypeIds = new long[] { 5 },
            StatusIds = new long[] { 1, 2 },
            PriorityIds = new[] { 3, 4 },
            OrganizationIds = new long[] { 6 },
            SupportingDepartmentIds = new long[] { 7 },
            EmployeeIds = new long[] { 8 },
            HasOpenAssignment = false,
            ReportedByUserIds = new long[] { 9 },
            ReportedFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ReportedTo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        [Theory]
        [InlineData(null, "asc")]
        [InlineData("referenceNumber", "asc")]
        [InlineData("equipment", "desc")]
        [InlineData("failureType", "asc")]
        [InlineData("failureCategory", "asc")]
        [InlineData("reportType", "asc")]
        [InlineData("status", "asc")]
        [InlineData("organization", "asc")]
        [InlineData("priority", "desc")]
        [InlineData("reportedDateTime", "desc")]
        [InlineData("createdDateTime", "desc")]
        public async Task The_list_and_export_query_with_every_filter_and_each_sort_translates_to_sql(string? sortBy, string direction)
        {
            var (service, exporter, context) = NewService();
            await using var _ = context;

            var request = EveryFilter();
            request.SortBy = sortBy;
            request.SortDirection = direction;
            await service.ExportAsync(request);

            var sql = exporter.Rows!.ToQueryString();
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("[PersonnelCode]", sql);          // the reporter's person set is part of the same query
            Assert.DoesNotContain("1 = 0", sql);
        }

        [Fact]
        public async Task The_inactive_export_lifts_the_soft_delete_filter_and_the_active_one_keeps_it()
        {
            var (service, exporter, context) = NewService();
            await using var _ = context;

            await service.ExportAsync(new FailureReportListRequestDto { IsActive = false });
            Assert.Contains("[IsActive] = CAST(0 AS bit)", exporter.Rows!.ToQueryString());

            await service.ExportAsync(new FailureReportListRequestDto());
            Assert.Contains("[IsActive] = CAST(1 AS bit)", exporter.Rows!.ToQueryString());
        }
    }
}
