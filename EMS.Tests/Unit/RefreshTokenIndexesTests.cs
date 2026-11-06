using EMS.Application.Common.Interfaces;
using EMS.Domain.Entities;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EMS.Tests.Unit
{
    /// <summary>Refresh and logout look a token up by its hash on every call: the column must be indexed (unique), and the clean-up job's date columns too.</summary>
    public class RefreshTokenIndexesTests
    {
        private sealed class NoUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        [Fact]
        public void The_refresh_token_table_is_indexed_for_the_lookup_and_the_cleanup()
        {
            var interceptor = new AuditSaveChangesInterceptor(new NoUser(), new HttpContextAccessor(),
                Options.Create(new AuditTrailOptions()), NullLogger<AuditSaveChangesInterceptor>.Instance);
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            using var context = new EMSDbContext(options, interceptor);
            var entity = context.Model.FindEntityType(typeof(UserRefreshToken))!;

            bool Indexed(string property, bool unique) => entity.GetIndexes().Any(i => i.Properties.Count == 1 && i.Properties[0].Name == property && i.IsUnique == unique);
            Assert.True(Indexed(nameof(UserRefreshToken.TokenHash), unique: true));
            Assert.True(Indexed(nameof(UserRefreshToken.ExpiresDateTime), unique: false));
            Assert.True(Indexed(nameof(UserRefreshToken.RevokedDateTime), unique: false));
        }
    }
}
