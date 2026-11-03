using EMS.Application.Common.Export;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Services.Equipment;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// The integration tests run on the in-memory provider, which evaluates queries as plain LINQ and so cannot reveal a query
    /// SQL Server cannot translate. This builds the real SQL Server provider (without connecting) and asks EF to translate the
    /// equipment queries: a construct the provider cannot translate throws here instead of in production.
    /// </summary>
    public class EquipmentSqlTranslationTests
    {
        private readonly Xunit.Abstractions.ITestOutputHelper _output;
        public EquipmentSqlTranslationTests(Xunit.Abstractions.ITestOutputHelper output) => _output = output;

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

        private static EMSDbContext NewSqlServerContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer("Server=localhost;Database=EmsTranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options,
                new AuditSaveChangesInterceptor(new NoUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));

        private static EquipmentListRequestDto EveryFilter() => new()
        {
            Search = "ab", YaasCode = "y", SerialNumber = "s", AssetNumber = "a",
            StatusIds = new long[] { 1, 2 }, CategoryIds = new long[] { 1 }, TypeIds = new long[] { 1, 2 }, ModelIds = new long[] { 3 }, BrandIds = new long[] { 4 },
            LocationTypeIds = new long[] { 5 }, OrganizationIds = new long[] { 6 }, FieldIds = new long[] { 7 }, OrganizationManagementIds = new long[] { 8 }, ZoneIds = new long[] { 9 },
            SupportingDepartmentIds = new long[] { 10 }, CityIds = new long[] { 11 }, ProvinceIds = new long[] { 12 }, TechnicianUserIds = new long[] { 13 },
            CreatedFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), CreatedTo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        private static (EquipmentService Service, CapturingExporter Exporter, EMSDbContext Context) NewService()
        {
            var context = NewSqlServerContext();
            var exporter = new CapturingExporter();
            var service = new EquipmentService(
                new EquipmentRepository(context), new EquipmentStatusRepository(context), new EquipmentTypeRepository(context), new EquipmentModelRepository(context),
                new EquipmentLocationTypeRepository(context), new SupportingDepartmentRepository(context), new Repository<EMS.Domain.Entities.Organization>(context),
                new PermissiveEquipmentLocationRule(), exporter, new NoUser(), new UnitOfWork(context), new NoAudit());
            return (service, exporter, context);
        }

        private sealed class NoAudit : IAuditEventLogger
        {
            public void Created(string entityType, object? entityId) { }
            public void Updated(string entityType, object? entityId) { }
            public void Deleted(string entityType, object? entityId) { }
            public void Operation(string operation, string entityType, object? entityId, string? detail = null) { }
        }

        [Theory]
        [InlineData(null, "asc")]
        [InlineData("yaasCode", "asc")]
        [InlineData("category", "desc")]
        [InlineData("type", "asc")]
        [InlineData("model", "asc")]
        [InlineData("brand", "asc")]
        [InlineData("status", "asc")]
        [InlineData("locationType", "asc")]
        [InlineData("organization", "asc")]
        [InlineData("department", "asc")]
        [InlineData("city", "asc")]
        [InlineData("province", "asc")]
        [InlineData("technician", "desc")]
        [InlineData("createdDateTime", "desc")]
        public async Task The_list_query_with_every_filter_and_each_sort_translates_to_sql(string? sortBy, string direction)
        {
            var (service, exporter, context) = NewService();
            await using var _ = context;

            var request = EveryFilter();
            request.SortBy = sortBy;
            request.SortDirection = direction;
            await service.ExportAsync(request);

            var sql = exporter.Rows!.ToQueryString();
            Assert.Contains("[YaasCode]", sql);
            Assert.Contains("ORDER BY", sql);
            Assert.DoesNotContain("1 = 0", sql);
        }

        [Fact]
        public async Task The_inactive_list_translates_and_lifts_the_soft_delete_filter()
        {
            var (service, exporter, context) = NewService();
            await using var _ = context;

            await service.ExportAsync(new EquipmentListRequestDto { IsActive = false });
            var sql = exporter.Rows!.ToQueryString();
            Assert.Contains("[IsActive] = CAST(0 AS bit)", sql);
        }

        [Fact]
        public async Task The_active_list_keeps_the_soft_delete_filter()
        {
            var (service, exporter, context) = NewService();
            await using var _ = context;

            await service.ExportAsync(new EquipmentListRequestDto { StatusIds = new long[] { 1, 2 }, TypeIds = new long[] { 3 }, Search = "x" });
            var sql = exporter.Rows!.ToQueryString();
            _output.WriteLine(sql);                       // visible with --logger "console;verbosity=detailed"
            Assert.Contains("[IsActive] = CAST(1 AS bit)", sql);
        }

        [Fact]
        public void The_detail_history_technician_and_yaas_queries_translate_to_sql()
        {
            using var context = NewSqlServerContext();
            var repository = new EquipmentRepository(context);

            Assert.Contains("[Equipments]", repository.QueryRows(true).Where(x => x.Id == 1).ToQueryString());
            Assert.Contains("[EquipmentLocationHistories]", repository.QueryLocationHistory(1).ToQueryString());
            Assert.Contains("[AspNetUsers]", repository.QueryTechnicians().Where(t => t.FullName.Contains("a") || t.PersonnelCode.Contains("a")).ToQueryString());
        }
    }
}
