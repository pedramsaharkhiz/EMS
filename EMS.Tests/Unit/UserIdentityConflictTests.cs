using EMS.Application.Common.Exceptions;
using EMS.Domain;
using EMS.Infrastructure.Persistence;
using EMS.Tests.Support;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection;

namespace EMS.Tests.Unit;

public class UserIdentityConflictTests
{
    // Simulate a SQL Server unique-index error; no SQL Server is used by this test.
    private static SqlException Duplicate(string index)
    {
        var errors = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        var ctor = typeof(SqlError).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .First(c => c.GetParameters().Length == 9);
        var error = (SqlError)ctor.Invoke(new object?[] { 2601, (byte)1, (byte)14, "test", "Duplicate index '" + index + "', value 'PRIVATE'.", "", 1, Convert.ChangeType(0, ctor.GetParameters()[7].ParameterType), null });
        typeof(SqlErrorCollection).GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(errors, new object[] { error });
        return (SqlException)typeof(SqlException).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .First(m => m.Name == "CreateException" && m.GetParameters().Length == 2)
            .Invoke(null, new object[] { errors, "test" })!;
    }

    private sealed class DuplicateInterceptor(SqlException error) : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) =>
            throw new DbUpdateException("provider error", error);
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("provider error", error);
    }

    [Theory]
    [InlineData(UserIdentityRules.PersonnelIndex, "personnel_code_taken")]
    [InlineData(UserIdentityRules.NationalIndex, "national_code_taken")]
    public async Task Identity_direct_saves_translate_race_errors_without_exposing_values(string index, string code)
    {
        var options = new DbContextOptionsBuilder<EMSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new DuplicateInterceptor(Duplicate(index))).Options;
        await using var db = new EMSDbContext(options, TestDb.NewInterceptor());
        var asyncError = await Assert.ThrowsAsync<ConflictException>(() => db.SaveChangesAsync());
        Assert.Equal(code, asyncError.Code);
        Assert.DoesNotContain("PRIVATE", asyncError.Message);
        var syncError = Assert.Throws<ConflictException>(() => db.SaveChanges());
        Assert.Equal(code, syncError.Code);
        Assert.Equal(asyncError.Message, syncError.Message);
    }
}
