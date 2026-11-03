using EMS.Application.Dtos.OrganizationHierarchy;
using EMS.Application.Validators.OrganizationHierarchy;
using Xunit;

namespace EMS.Tests.Unit;

public sealed class OrganizationHierarchyValidationTests
{
    private readonly OrganizationHierarchyRequestDtoValidator _validator = new();

    [Theory]
    [InlineData("zone")]
    [InlineData("organizationManagement")]
    [InlineData("field")]
    [InlineData("organization")]
    public void Valid_search_levels_are_accepted(string level)
    {
        var request = CreateRequest();
        request.SearchLevel = level;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Missing_search_level_is_rejected()
    {
        var request = CreateRequest();
        request.SearchLevel = "";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Parent_id_without_parent_level_is_rejected()
    {
        var request = CreateRequest();
        request.ParentId = 12;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Parent_level_without_id_is_valid()
    {
        var request = CreateRequest();
        request.ParentLevel = "organizationManagement";

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("zone", "zone")]
    [InlineData("field", "field")]
    [InlineData("field", "organization")]
    [InlineData("organizationManagement", "field")]
    public void Same_or_lower_parent_level_is_rejected(
        string searchLevel,
        string parentLevel)
    {
        var request = CreateRequest();
        request.SearchLevel = searchLevel;
        request.ParentLevel = parentLevel;
        request.ParentId = 1;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("organization", "zone")]
    [InlineData("organization", "organizationManagement")]
    [InlineData("organization", "field")]
    [InlineData("field", "zone")]
    [InlineData("field", "organizationManagement")]
    [InlineData("organizationManagement", "zone")]
    public void Valid_parent_combinations_are_accepted(
        string searchLevel,
        string parentLevel)
    {
        var request = CreateRequest();
        request.SearchLevel = searchLevel;
        request.ParentLevel = parentLevel;
        request.ParentId = 1;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("zone", "field")]
    [InlineData("zone", "parent")]
    [InlineData("field", "field")]
    [InlineData("organizationManagement", "organizationManagement")]
    public void Invalid_group_combinations_are_rejected(
        string searchLevel,
        string groupBy)
    {
        var request = CreateRequest();
        request.SearchLevel = searchLevel;
        request.GroupBy = groupBy;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("zone", "isActive")]
    [InlineData("organizationManagement", "zone")]
    [InlineData("field", "parent")]
    [InlineData("organization", "field")]
    [InlineData("organization", "parent")]
    [InlineData("organization", "organizationManagement")]
    public void Valid_group_combinations_are_accepted(
        string searchLevel,
        string groupBy)
    {
        var request = CreateRequest();
        request.SearchLevel = searchLevel;
        request.GroupBy = groupBy;

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Count_sort_is_rejected_in_list_mode()
    {
        var request = CreateRequest();
        request.SortBy = "count";

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Count_sort_is_accepted_in_group_mode()
    {
        var request = CreateRequest();
        request.GroupBy = "field";
        request.SortBy = "count";

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Title_sort_is_rejected_in_group_mode()
    {
        var request = CreateRequest();
        request.GroupBy = "field";
        request.SortBy = "title";

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Large_page_offset_is_rejected()
    {
        var request = CreateRequest();
        request.PageNumber = int.MaxValue;
        request.PageSize = 100;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Invalid_page_size_is_rejected(int pageSize)
    {
        var request = CreateRequest();
        request.PageSize = pageSize;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Names_are_case_insensitive_and_trimmed()
    {
        var request = CreateRequest();
        request.SearchLevel = " Organization ";
        request.ParentLevel = " ZONE ";
        request.ParentId = 1;
        request.GroupBy = " FIELD ";
        request.SortBy = " COUNT ";
        request.SortDirection = " DESC ";

        Assert.True(_validator.Validate(request).IsValid);
    }

    private static OrganizationHierarchyRequestDto CreateRequest()
    {
        return new OrganizationHierarchyRequestDto
        {
            SearchLevel = "organization",
            PageNumber = 1,
            PageSize = 20,
            SortDirection = "asc"
        };
    }
}
