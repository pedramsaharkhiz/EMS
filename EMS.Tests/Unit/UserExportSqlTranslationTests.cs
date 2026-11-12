using EMS.Application.Common.Export;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Dtos.User;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using EMS.Infrastructure.Services.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit
{
    /// <summary>
    /// The users export rows (place of work, and the roles and skills as one text each) must translate with the real SQL Server provider
    /// for every filter of the users list; the in-memory provider used by the integration tests cannot show a query SQL Server rejects.
    /// </summary>
    public class UserExportSqlTranslationTests
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

        private static EMSDbContext NewContext() =>
            new(new DbContextOptionsBuilder<EMSDbContext>().UseSqlServer("Server=localhost;Database=EmsTranslationOnly;Trusted_Connection=True;TrustServerCertificate=True").Options,
                new AuditSaveChangesInterceptor(new NoUser(), new Microsoft.AspNetCore.Http.HttpContextAccessor(),
                    Microsoft.Extensions.Options.Options.Create(new AuditTrailOptions()),
                    Microsoft.Extensions.Logging.Abstractions.NullLogger<AuditSaveChangesInterceptor>.Instance));

        public static IEnumerable<object[]> Requests()
        {
            yield return new object[] { new UserListRequestDto() };
            yield return new object[] { new UserListRequestDto { IsActive = false } };
            yield return new object[] { new UserListRequestDto { Search = "12", RoleId = 3, SkillId = 4 } };
            yield return new object[] { new UserListRequestDto { HasRole = false, HasSkill = true } };
            yield return new object[]
            {
                new UserListRequestDto
                {
                    Id = 1, UserName = "u", FirstName = "f", LastName = "l", PersonnelCode = "p", NationalCode = "n", PhoneNumber = "09", Email = "@",
                    SupportingDepartmentId = 5, PostId = 6, IsActive = true,
                },
            };
        }

        [Theory]
        [MemberData(nameof(Requests))]
        public async Task The_export_query_translates_to_sql_for_every_filter(UserListRequestDto request)
        {
            await using var context = NewContext();
            var exporter = new CapturingExporter();
            var service = new UserManagementService(userManager: null!, context, new NoAudit(), exporter, new EMS.Tests.Support.PassThroughRowAccess());

            await service.ExportAsync(request);

            var sql = exporter.Rows!.ToQueryString();
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("[PersonnelCode]", sql);
            foreach (var secret in new[] { "PasswordHash", "SecurityStamp", "ConcurrencyStamp" })
                Assert.DoesNotContain(secret, sql);          // nothing secret is even read from the database
        }
    }
}
