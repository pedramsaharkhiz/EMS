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

    public IReadOnlyList<OrganizationHierarchyReferenceDto> Ancestors
    {
        get;
        init;
    } = [];
}

public sealed class OrganizationHierarchyGroupDto
{
    public string? Key { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public int Count { get; init; }
}

public sealed class OrganizationHierarchyResponseDto
{
    public string Mode { get; init; } = "list";

    public IReadOnlyList<OrganizationHierarchyItemDto>? Items
    {
        get;
        init;
    }

    public IReadOnlyList<OrganizationHierarchyGroupDto>? Groups
    {
        get;
        init;
    }
}

// نتیجه داخلی سرویس؛ Controller اطلاعات صفحه‌بندی را
// در meta.pagination قرار می‌دهد.
public sealed class OrganizationHierarchyResult
{
    public OrganizationHierarchyResponseDto Data { get; init; } = new();

    public ApiPagination Pagination { get; init; } = new();
}
