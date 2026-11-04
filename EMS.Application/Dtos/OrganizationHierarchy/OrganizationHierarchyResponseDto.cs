using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.OrganizationHierarchy;

public sealed class OrganizationHierarchyReferenceDto
{
    public long Id { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Code { get; init; }
}

public sealed class OrganizationHierarchyItemDto
{
    public long Id { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Code { get; init; }

    public bool IsActive { get; init; }

    public OrganizationHierarchyReferenceDto? Parent { get; init; }

    // اجداد بالاتر از والد مستقیم، از بالاترین سطح به پایین.
    public IReadOnlyList<OrganizationHierarchyReferenceDto> Ancestors
    {
        get;
        init;
    } = [];
}

public sealed class OrganizationHierarchyResponseDto
{
    public IReadOnlyList<OrganizationHierarchyItemDto> Items
    {
        get;
        init;
    } = [];
}

// نتیجه داخلی سرویس.
// Controller اطلاعات Pagination را در meta.pagination قرار می‌دهد.
public sealed class OrganizationHierarchyResult
{
    public OrganizationHierarchyResponseDto Data { get; init; } = new();

    public ApiPagination Pagination { get; init; } = new();
}
