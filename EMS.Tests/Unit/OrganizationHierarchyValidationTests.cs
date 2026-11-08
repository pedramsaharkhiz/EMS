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
    [InlineData("employee")]
    public void Valid_search_levels_are_accepted(string level)
    {
        var request = CreateRequest();
        request.SearchLevel = level;

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    public void Invalid_search_levels_are_rejected(string level)
    {
        var request = CreateRequest();
        request.SearchLevel = level;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Parent_id_without_parent_level_is_rejected()
    {
        var request = CreateRequest();
        request.ParentId = 12;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Parent_level_without_id_is_valid()
    {
        var request = CreateRequest();
        request.ParentLevel = "organizationManagement";

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("zone", "zone")]
    [InlineData("field", "field")]
    [InlineData("field", "organization")]
    [InlineData("organizationManagement", "field")]
    [InlineData("organization", "employee")]
    [InlineData("employee", "employee")]
    public void Same_or_lower_parent_level_is_rejected(
        string searchLevel,
        string parentLevel)
    {
        var request = CreateRequest();
        request.SearchLevel = searchLevel;
        request.ParentLevel = parentLevel;
        request.ParentId = 1;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData("organization", "zone")]
    [InlineData("organization", "organizationManagement")]
    [InlineData("organization", "field")]
    [InlineData("field", "zone")]
    [InlineData("field", "organizationManagement")]
    [InlineData("organizationManagement", "zone")]
    [InlineData("employee", "organization")]
    [InlineData("employee", "field")]
    [InlineData("employee", "organizationManagement")]
    [InlineData("employee", "zone")]
    public void Valid_parent_combinations_are_accepted(
        string searchLevel,
        string parentLevel)
    {
        var request = CreateRequest();
        request.SearchLevel = searchLevel;
        request.ParentLevel = parentLevel;
        request.ParentId = 1;

        Assert.True(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Unknown_parent_level_is_rejected()
    {
        var request = CreateRequest();
        request.ParentLevel = "invalid";
        request.ParentId = 1;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_parent_id_is_rejected(long parentId)
    {
        var request = CreateRequest();
        request.ParentLevel = "field";
        request.ParentId = parentId;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Invalid_page_number_is_rejected(int pageNumber)
    {
        var request = CreateRequest();
        request.PageNumber = pageNumber;

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
    public void Large_page_offset_is_rejected()
    {
        var request = CreateRequest();
        request.PageNumber = int.MaxValue;
        request.PageSize = 100;

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void Names_are_case_insensitive_and_trimmed()
    {
        var request = CreateRequest();
        request.SearchLevel = " Organization ";
        request.ParentLevel = " ZONE ";
        request.ParentId = 1;

        Assert.True(_validator.Validate(request).IsValid);
    }

    private static OrganizationHierarchyRequestDto CreateRequest()
    {
        return new OrganizationHierarchyRequestDto
        {
            SearchLevel = "organization",
            PageNumber = 1,
            PageSize = 20
        };
    }
}
