using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Maintenance;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EMS.Tests.Unit
{
    public class RefreshTokenCleanupTests
    {
        private sealed class FakeUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private static EMSDbContext NewContext()
        {
            var interceptor = new AuditSaveChangesInterceptor(new FakeUser(), new HttpContextAccessor(),
                Options.Create(new AuditTrailOptions()), NullLogger<AuditSaveChangesInterceptor>.Instance);
            var options = new DbContextOptionsBuilder<EMSDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).AddInterceptors(interceptor).Options;
            return new EMSDbContext(options, interceptor);
        }

        private static UserRefreshToken Token(string hash, DateTime expires, DateTime? revoked = null) => new()
        {
            UserId = 1, TokenHash = hash, CreatedByIp = "127.0.0.1", ExpiresDateTime = expires, RevokedDateTime = revoked
        };

        [Fact]
        public async Task Removes_only_tokens_that_expired_or_were_revoked_before_the_retention_period()
        {
            var now = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
            await using var db = NewContext();
            db.UserRefreshTokens.AddRange(
                Token("expired-long-ago", now.AddDays(-40)),
                Token("revoked-long-ago", now.AddDays(+5), revoked: now.AddDays(-40)),
                Token("expired-recently", now.AddDays(-3)),
                Token("revoked-recently", now.AddDays(+5), revoked: now.AddDays(-3)),
                Token("still-valid", now.AddDays(+5)));
            await db.SaveChangesAsync();

            var removed = await RefreshTokenCleanupBackgroundService.CleanupAsync(db, new RefreshTokenCleanupOptions { RetentionDays = 30 }, now, default);

            Assert.Equal(2, removed);
            var left = (await db.UserRefreshTokens.Select(t => t.TokenHash).ToListAsync()).OrderBy(x => x).ToList();
            Assert.Equal(new[] { "expired-recently", "revoked-recently", "still-valid" }, left);
        }

        [Fact]
        public async Task Works_through_large_backlogs_in_batches()
        {
            var now = DateTime.UtcNow;
            await using var db = NewContext();
            for (var i = 0; i < 25; i++)
                db.UserRefreshTokens.Add(Token("old" + i, now.AddDays(-90)));
            db.UserRefreshTokens.Add(Token("keep", now.AddDays(1)));
            await db.SaveChangesAsync();

            var removed = await RefreshTokenCleanupBackgroundService.CleanupAsync(db, new RefreshTokenCleanupOptions { RetentionDays = 30, BatchSize = 10 }, now, default);

            Assert.Equal(25, removed);
            Assert.Equal("keep", (await db.UserRefreshTokens.SingleAsync()).TokenHash);
        }
    }
}
