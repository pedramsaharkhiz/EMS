using EMS.Application.Common.Exceptions;
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
    public class UnitOfWorkTests
    {
        private sealed class FakeUser : ICurrentUserService
        {
            public long? UserId => 1;
            public string? UserName => "t";
            public bool IsAuthenticated => true;
            public IEnumerable<string> Roles => Array.Empty<string>();
            public IEnumerable<string> Permissions => Array.Empty<string>();
        }

        private static EMSDbContext NewContext(string db)
        {
            var interceptor = new AuditSaveChangesInterceptor(new FakeUser(), new HttpContextAccessor(),
                Options.Create(new AuditTrailOptions()), NullLogger<AuditSaveChangesInterceptor>.Instance);
            var options = new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(db).AddInterceptors(interceptor).Options;
            return new EMSDbContext(options, interceptor);
        }

        [Fact]
        public async Task A_lost_update_becomes_a_409_conflict_not_a_server_error()
        {
            var db = Guid.NewGuid().ToString("N");
            long id;
            await using (var seed = NewContext(db))
            {
                var brand = new Brand { Title = "Original", Code = "O" };
                seed.Brands.Add(brand);
                await seed.SaveChangesAsync();
                id = brand.BrandId;
                brand.Code = "first-edit";            // gives the row a ModifiedDateTime
                await seed.SaveChangesAsync();
            }

            await using var userA = NewContext(db);
            await using var userB = NewContext(db);
            var brandA = await userA.Brands.SingleAsync(b => b.BrandId == id);
            var brandB = await userB.Brands.SingleAsync(b => b.BrandId == id);

            brandA.Code = "A";
            await new UnitOfWork(userA).SaveChangesAsync();

            brandB.Code = "B";
            await Assert.ThrowsAsync<ConflictException>(() => new UnitOfWork(userB).SaveChangesAsync());
        }
    }
}
