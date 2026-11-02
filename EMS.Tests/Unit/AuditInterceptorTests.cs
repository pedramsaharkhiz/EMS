using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Identity;
using EMS.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EMS.Tests.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EMS.Tests.Unit
{
    public class AuditInterceptorTests
    {
        private sealed class FakeCurrentUser : ICurrentUserService
        {
            public long? UserId => 42;
            public string? UserName => "tester";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private static EMSDbContext CreateContext(bool auditTrailEnabled, string databaseName)
        {
            var http = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "corr-12345678" }
            };
            var interceptor = new AuditSaveChangesInterceptor(
                new FakeCurrentUser(),
                http,
                Options.Create(new AuditTrailOptions { Enabled = auditTrailEnabled }),
                NullLogger<AuditSaveChangesInterceptor>.Instance);

            var options = new DbContextOptionsBuilder<EMSDbContext>()
                .UseInMemoryDatabase(databaseName)
                .AddInterceptors(interceptor)
                .Options;

            return new EMSDbContext(options, interceptor);
        }

        private static string NewDb() => Guid.NewGuid().ToString("N");

        [Fact]
        public async Task Insert_is_recorded_with_generated_key_new_values_user_and_correlation_id()
        {
            await using var ctx = CreateContext(true, NewDb());
            var brand = new Brand { Title = "Siemens", Code = "SI" };
            ctx.Brands.Add(brand);
            await ctx.SaveChangesAsync();

            var row = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.Equal("Insert", row.Action);
            Assert.Contains("Brand", row.TableName);
            Assert.Equal(brand.BrandId.ToString(), row.EntityId);
            Assert.Null(row.OldValues);
            Assert.Equal(42, row.UserId);
            Assert.Equal("tester", row.UserName);
            Assert.Equal("corr-12345678", row.CorrelationId);

            using var newValues = JsonDocument.Parse(row.NewValues!);
            Assert.Equal("Siemens", newValues.RootElement.GetProperty("Title").GetString());
            Assert.Equal(brand.BrandId, newValues.RootElement.GetProperty("BrandId").GetInt64());
        }

        [Fact]
        public async Task Update_records_only_the_columns_that_really_changed_with_old_and_new_values()
        {
            await using var ctx = CreateContext(true, NewDb());
            var brand = new Brand { Title = "Old title", Code = "C1" };
            ctx.Brands.Add(brand);
            await ctx.SaveChangesAsync();
            ctx.AuditLogs.RemoveRange(ctx.AuditLogs);
            await ctx.SaveChangesAsync();

            brand.Title = "New title";
            ctx.Brands.Update(brand); // marks every column as modified, like the repositories do
            await ctx.SaveChangesAsync();

            var row = Assert.Single(await ctx.AuditLogs.Where(a => a.Action == "Update").ToListAsync());
            Assert.Equal("Title", row.ChangedColumns);
            Assert.Equal(brand.BrandId.ToString(), row.EntityId);

            using var oldValues = JsonDocument.Parse(row.OldValues!);
            using var newValues = JsonDocument.Parse(row.NewValues!);
            Assert.Equal("Old title", oldValues.RootElement.GetProperty("Title").GetString());
            Assert.Equal("New title", newValues.RootElement.GetProperty("Title").GetString());
            Assert.False(newValues.RootElement.TryGetProperty("ModifiedDateTime", out _));
        }

        [Fact]
        public async Task An_update_that_changes_nothing_is_not_recorded()
        {
            await using var ctx = CreateContext(true, NewDb());
            var brand = new Brand { Title = "Same", Code = "S" };
            ctx.Brands.Add(brand);
            await ctx.SaveChangesAsync();
            ctx.AuditLogs.RemoveRange(ctx.AuditLogs);
            await ctx.SaveChangesAsync();

            ctx.Brands.Update(brand);
            await ctx.SaveChangesAsync();

            Assert.Empty(await ctx.AuditLogs.ToListAsync());
        }

        [Fact]
        public async Task Delete_records_the_old_values()
        {
            await using var ctx = CreateContext(true, NewDb());
            var brand = new Brand { Title = "Doomed", Code = "D" };
            ctx.Brands.Add(brand);
            await ctx.SaveChangesAsync();
            var id = brand.BrandId;

            ctx.Brands.Remove(brand);
            await ctx.SaveChangesAsync();

            var row = Assert.Single(await ctx.AuditLogs.Where(a => a.Action == "Delete").ToListAsync());
            Assert.Equal(id.ToString(), row.EntityId);
            Assert.Null(row.NewValues);
            using var oldValues = JsonDocument.Parse(row.OldValues!);
            Assert.Equal("Doomed", oldValues.RootElement.GetProperty("Title").GetString());
        }

        [Fact]
        public async Task Sensitive_columns_are_masked_and_never_stored_in_clear_text()
        {
            await using var ctx = CreateContext(true, NewDb());
            ctx.Users.Add(new ApplicationUser
            {
                UserName = "alice",
                FirstName = "Alice",
                LastName = "A",
                PersonnelCode = "P1",
                PasswordHash = "SUPER-SECRET-HASH-VALUE",
                SecurityStamp = "SECRET-STAMP"
            });
            await ctx.SaveChangesAsync();

            var row = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.DoesNotContain("SUPER-SECRET-HASH-VALUE", row.NewValues);
            Assert.DoesNotContain("SECRET-STAMP", row.NewValues);
            using var newValues = JsonDocument.Parse(row.NewValues!);
            Assert.Equal("***", newValues.RootElement.GetProperty("PasswordHash").GetString());
            Assert.Equal("***", newValues.RootElement.GetProperty("SecurityStamp").GetString());
            Assert.Equal("alice", newValues.RootElement.GetProperty("UserName").GetString());
        }

        [Fact]
        public async Task Nothing_is_written_when_the_audit_trail_is_disabled()
        {
            await using var ctx = CreateContext(false, NewDb());
            ctx.Brands.Add(new Brand { Title = "Quiet", Code = "Q" });
            await ctx.SaveChangesAsync();

            Assert.Empty(await ctx.AuditLogs.ToListAsync());
            // The Created/Modified stamps must still be applied.
            var brand = await ctx.Brands.SingleAsync();
            Assert.Equal(42, brand.CreatedByUserId);
        }

        /// <summary>Simulates the AuditLogs table being unavailable (e.g. migration not applied).</summary>
        private sealed class FailWhenWritingAuditLogs : SaveChangesInterceptor
        {
            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                if (eventData.Context!.ChangeTracker.Entries<AuditLog>().Any(e => e.State == EntityState.Added))
                    throw new InvalidOperationException("Invalid object name 'AuditLogs'.");
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        [Fact]
        public async Task A_failing_audit_trail_write_never_breaks_the_business_operation_and_is_logged()
        {
            var logs = new ListLoggerFactory();
            var interceptor = new AuditSaveChangesInterceptor(
                new FakeCurrentUser(),
                new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
                Options.Create(new AuditTrailOptions { Enabled = true }),
                logs.CreateLogger<AuditSaveChangesInterceptor>());
            var options = new DbContextOptionsBuilder<EMSDbContext>()
                .UseInMemoryDatabase(NewDb())
                .AddInterceptors(interceptor, new FailWhenWritingAuditLogs())
                .Options;

            await using var ctx = new EMSDbContext(options, interceptor);
            ctx.Brands.Add(new Brand { Title = "Survives", Code = "S" });

            var saved = await ctx.SaveChangesAsync();   // must not throw

            Assert.Equal(1, saved);
            Assert.Equal("Survives", (await ctx.Brands.SingleAsync()).Title);
            Assert.Empty(ctx.ChangeTracker.Entries<AuditLog>());   // failed audit rows are not left tracked

            var entry = Assert.Single(logs.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Contains("Failed to write 1 audit trail row", entry.Message);
            Assert.NotNull(entry.Exception);

            // ...and the next save works normally again.
            ctx.Brands.Add(new Brand { Title = "Second", Code = "S2" });
            Assert.Equal(1, await ctx.SaveChangesAsync());
        }

        [Fact]
        public async Task Refresh_tokens_are_never_audited()
        {
            await using var ctx = CreateContext(true, NewDb());
            ctx.UserRefreshTokens.Add(new UserRefreshToken
            {
                UserId = 1,
                TokenHash = "hash",
                CreatedByIp = "127.0.0.1",
                ExpiresDateTime = DateTime.UtcNow.AddDays(1)
            });
            await ctx.SaveChangesAsync();

            Assert.Empty(await ctx.AuditLogs.ToListAsync());
        }
    }
}
