using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EMS.Tests.Unit
{
    public class RefreshTokenConcurrencyTests
    {
        private sealed class NoUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private static EMSDbContext NewContext(string db)
        {
            var interceptor = new AuditSaveChangesInterceptor(new NoUser(), new HttpContextAccessor(),
                Options.Create(new AuditTrailOptions()), NullLogger<AuditSaveChangesInterceptor>.Instance);
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(db).AddInterceptors(interceptor).Options;
            return new EMSDbContext(options, interceptor);
        }

        [Fact]
        public async Task A_refresh_token_can_be_consumed_by_only_one_of_two_concurrent_requests()
        {
            var db = Guid.NewGuid().ToString("N");
            await using (var seed = NewContext(db))
            {
                seed.UserRefreshTokens.Add(new UserRefreshToken
                {
                    UserId = 1, TokenHash = "h", CreatedByIp = "127.0.0.1", ExpiresDateTime = DateTime.UtcNow.AddDays(1)
                });
                await seed.SaveChangesAsync();
            }

            // Two requests read the token while it is still unrevoked...
            await using var first = NewContext(db);
            await using var second = NewContext(db);
            var tokenInFirst = await first.UserRefreshTokens.SingleAsync();
            var tokenInSecond = await second.UserRefreshTokens.SingleAsync();

            // ...both try to consume it: the first write wins, the second must fail instead of also succeeding.
            tokenInFirst.RevokedDateTime = DateTime.UtcNow;
            await first.SaveChangesAsync();

            tokenInSecond.RevokedDateTime = DateTime.UtcNow;
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
        }
    }
}
