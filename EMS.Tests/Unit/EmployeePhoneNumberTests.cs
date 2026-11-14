using EMS.Application.Common.DataAccess;
using EMS.Application.Dtos.EmployeePhoneNumber;
using EMS.Application.Security.RowPolicies;
using EMS.Application.Validators.EmployeePhoneNumber;
using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using EMS.Infrastructure.Repositories;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace EMS.Tests.Unit;

public class EmployeePhoneNumberTests
{
    [Theory]
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹", "09123456789")]
    [InlineData("٠٩١٢٣٤٥٦٧٨٩", "09123456789")]
    [InlineData(" +989123456789 ", "09123456789")]
    [InlineData("00989123456789", "09123456789")]
    public void Supported_formats_are_canonical_and_valid(string input, string expected)
    { Assert.Equal(expected, EmployeePhoneRules.Normalize(input)); Assert.True(EmployeePhoneRules.IsValid(input)); }

    [Fact]
    public void Schema_enforces_global_ownership_and_concurrency_without_cascading_deletes()
    {
        using var db = TestDb.SqlServerModelOnly(); var entity = db.Model.FindEntityType(typeof(EmployeePhoneNumber))!;
        var index = entity.GetIndexes().Single(i => i.GetDatabaseName() == EmployeePhoneRules.UniqueIndex);
        Assert.True(index.IsUnique); Assert.Null(index.GetFilter());
        Assert.Equal(new[] { "PhoneNumber" }, index.Properties.Select(p => p.Name));
        Assert.Equal(11, entity.FindProperty("PhoneNumber")!.GetMaxLength());
        Assert.True(entity.FindProperty("ModifiedDateTime")!.IsConcurrencyToken);
        Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.NoAction, fk.DeleteBehavior));
        Assert.Equal(EmployeePhoneRules.TakenCode, UniqueConstraintMessages.Find(EmployeePhoneRules.UniqueIndex)!.Value.Code);
    }
    [Fact]
    public void Contact_scope_and_queries_translate_without_a_sql_connection()
    {
        using var db = TestDb.SqlServerModelOnly();
        var scope = DataScope.Restricted(1, new long[] { 7 }, "test");
        var filter = new EmployeeContactRowPolicy().Filter(scope)!;
        var rows = db.EmployeePhoneNumbers.IgnoreQueryFilters().Select(p => new EmployeeContactRow
        { EmployeeId = p.EmployeeId, EmployeePhoneNumberId = p.EmployeePhoneNumberId, SupportingDepartmentId = p.Employee.Organization.SupportingDepartmentId });
        var visible = rows.Where(filter).Select(p => p.EmployeePhoneNumberId);
        var sql = new EmployeePhoneNumberRepository(db).QueryAll().Where(p => visible.Contains(p.EmployeePhoneNumberId) && p.IsActive).ToQueryString();
        Assert.Contains("SupportingDepartmentId", sql);
        Assert.True(filter.Compile()(new() { SupportingDepartmentId = 7 }));
        Assert.False(filter.Compile()(new() { SupportingDepartmentId = 8 }));
        Assert.False(filter.Compile()(new()));
    }
    [Fact]
    public void Report_lists_are_optional_bounded_and_have_indexed_errors()
    {
        Assert.Empty(EmployeePhoneInputRules.Errors(null, null));
        Assert.Empty(EmployeePhoneInputRules.Errors(null, []));
        var errors = EmployeePhoneInputRules.Errors(null, [new() { PhoneNumber = "bad" }]);
        Assert.Contains(errors, e => e.PropertyName == "EmployeeId");
        Assert.Contains(errors, e => e.PropertyName == "EmployeePhoneNumbers[0].PhoneNumber");
        var many = Enumerable.Range(0, 21).Select(_ => new EmployeePhoneNumberInputDto { PhoneNumber = "09123456789" }).ToList();
        Assert.Contains(EmployeePhoneInputRules.Errors(1, many), e => e.PropertyName == "EmployeePhoneNumbers");
        Assert.Contains(EmployeePhoneInputRules.Errors(1, [new() { Id = 1, PhoneNumber = "09123456789" }, new() { Id = 1, PhoneNumber = "09351234567" }]), e => e.PropertyName == "EmployeePhoneNumbers[1].Id");
    }
}
