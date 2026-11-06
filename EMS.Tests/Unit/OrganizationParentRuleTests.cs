using EMS.Application.Dtos.Organization;
using EMS.Application.Validators.Organization;
using EMS.Domain;
using EMS.Domain.Entities;
using EMS.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace EMS.Tests.Unit;

/// <summary>A branch has exactly one parent: a field (حوزه) or a branch management (مدیریت شعب). docs/BUSINESS-DECISIONS.md, section 0.</summary>
public sealed class OrganizationParentRuleTests
{
    private readonly OrganizationValidator _validator = new();

    private static OrganizationDto Branch(long? fieldId, long? managementId) => new()
    {
        Title = "شعبه‌ی آزمایشی",
        OrganizationTypeId = 1,
        FieldId = fieldId,
        OrganizationManagementId = managementId
    };

    [Theory]
    [InlineData(5L, null)]
    [InlineData(null, 7L)]
    public void A_branch_with_exactly_one_parent_is_valid(long? fieldId, long? managementId) =>
        Assert.True(_validator.Validate(Branch(fieldId, managementId)).IsValid);

    [Theory]
    [InlineData(null, null)]
    [InlineData(5L, 7L)]
    public void A_branch_with_no_parent_or_both_parents_is_refused(long? fieldId, long? managementId)
    {
        var result = _validator.Validate(Branch(fieldId, managementId));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("دقیقاً یکی"));
    }

    [Fact]
    public void The_table_has_a_check_constraint_for_exactly_one_parent()
    {
        using var db = TestDb.SqlServerModelOnly();
        var organization = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Organization))!;

        var check = organization.GetCheckConstraints().Single(c => c.Name == OrganizationConstraints.ParentCheckName);

        Assert.Equal(OrganizationConstraints.ParentCheckSql, check.Sql);
        Assert.Contains("[FieldId] IS NOT NULL AND [OrganizationManagementId] IS NULL", check.Sql);
        Assert.Contains("[FieldId] IS NULL AND [OrganizationManagementId] IS NOT NULL", check.Sql);
    }
}
