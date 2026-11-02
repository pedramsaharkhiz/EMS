using EMS.Application.Common.Interfaces;
using EMS.Infrastructure.Options;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EMS.Tests.Support
{
    /// <summary>A fixed signed-in user for code that stamps "created by" / "modified by".</summary>
    public sealed class FakeCurrentUser : ICurrentUserService
    {
        public long? UserId => 42;
        public string? UserName => "tester";
        public bool IsAuthenticated => true;
        public IEnumerable<string> Roles => Array.Empty<string>();
        public IEnumerable<string> Permissions => Array.Empty<string>();
    }

    /// <summary>Builds an <see cref="EMSDbContext"/> wired like the application for unit tests.</summary>
    public static class TestDb
    {
        public static AuditSaveChangesInterceptor NewInterceptor(bool auditTrailEnabled = false, ICurrentUserService? user = null, IHttpContextAccessor? http = null) =>
            new(user ?? new FakeCurrentUser(),
                http ?? new HttpContextAccessor(),
                Options.Create(new AuditTrailOptions { Enabled = auditTrailEnabled }),
                NullLogger<AuditSaveChangesInterceptor>.Instance);

        /// <summary>A SQL Server model only for design-time metadata (no connection is ever opened).</summary>
        public static EMSDbContext SqlServerModelOnly()
        {
            var interceptor = NewInterceptor();
            var options = new DbContextOptionsBuilder<EMSDbContext>()
                .UseSqlServer("Server=(none);Database=EMS;Trusted_Connection=True;TrustServerCertificate=True")
                .AddInterceptors(interceptor)
                .Options;
            return new EMSDbContext(options, interceptor);
        }
    }
}
