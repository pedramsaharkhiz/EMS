using EMS.Domain.Entities;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Persistence.Seed;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EMS.Tests.Unit;

public class NoteCategorySeederTests
{
    private static EMSDbContext NewContext()
    {
        var interceptor = TestDb.NewInterceptor();
        return new EMSDbContext(new DbContextOptionsBuilder<EMSDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(interceptor).Options, interceptor);
    }

    [Fact]
    public async Task Adds_the_six_requested_titles_once_with_normal_catalog_records()
    {
        await using var db = NewContext();
        var seeder = new NoteCategorySeeder(db, NullLogger<NoteCategorySeeder>.Instance);
        Assert.Equal(6, await seeder.SeedAsync());
        Assert.Equal(0, await seeder.SeedAsync());
        var rows = await db.Set<NoteCategory>().ToListAsync();
        Assert.Equal(new[] { "پیگیری", "هشدار", "تذکر", "هماهنگی", "درخواست اطلاعات", "راهنمایی فنی" }.OrderBy(x => x),
            rows.Select(x => x.Title).OrderBy(x => x));
        Assert.All(rows, row =>
        {
            Assert.True(row.IsActive);
            Assert.True(row.NoteCategoryId > 0);
            Assert.Equal(0, row.CreatedByUserId);
            Assert.NotEqual(default, row.CreatedDateTime);
        });
    }

    [Fact]
    public async Task Existing_active_inactive_and_custom_records_keep_their_identity_and_audit_fields()
    {
        await using var db = NewContext();
        var active = new NoteCategory { Title = "پیگیری", CreatedByUserId = 7 };
        var inactive = new NoteCategory { Title = "هشدار", IsActive = false, CreatedByUserId = 8 };
        var custom = new NoteCategory { Title = "موضوع اختصاصی", CreatedByUserId = 9 };
        db.AddRange(active, inactive, custom);
        await db.SaveChangesAsync();
        var before = new[] { active, inactive, custom }.Select(x =>
            (x.NoteCategoryId, x.Title, x.IsActive, x.CreatedByUserId, x.CreatedDateTime, x.ModifiedDateTime)).ToArray();
        db.ChangeTracker.Clear();
        var seeder = new NoteCategorySeeder(db, NullLogger<NoteCategorySeeder>.Instance);
        Assert.Equal(4, await seeder.SeedAsync());
        Assert.Equal(0, await seeder.SeedAsync());
        foreach (var original in before)
        {
            var row = await db.Set<NoteCategory>().IncludingInactive().SingleAsync(x => x.NoteCategoryId == original.NoteCategoryId);
            Assert.Equal(original, (row.NoteCategoryId, row.Title, row.IsActive, row.CreatedByUserId, row.CreatedDateTime, row.ModifiedDateTime));
        }
        Assert.Equal(7, await db.Set<NoteCategory>().IncludingInactive().CountAsync());
        Assert.Single(await db.Set<NoteCategory>().IncludingInactive().Where(x => x.Title == "هشدار").ToListAsync());
    }
}
