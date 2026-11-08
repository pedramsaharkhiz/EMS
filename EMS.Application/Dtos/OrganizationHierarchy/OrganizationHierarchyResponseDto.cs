using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.OrganizationHierarchy;

public sealed class OrganizationHierarchyReferenceDto
{
    public long Id { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Code { get; init; }
}

/// <summary>The organizational structure (ساختار سازمانی / جایگاه شغلی) a bank employee belongs to.</summary>
public sealed class OrganizationStructureReferenceDto
{
    public long Id { get; init; }

    public string? Code { get; init; }

    public string? Title { get; init; }

    public int? Grade { get; init; }
}

public sealed class OrganizationHierarchyItemDto
{
    public long Id { get; init; }

    public string Level { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string? Code { get; init; }

    public bool IsActive { get; init; }

    public OrganizationHierarchyReferenceDto? Parent { get; init; }

    // فقط برای سطح organization: دپارتمان پشتیبانی شعبه (در سطح‌های دیگر null).
    public long? SupportingDepartmentId { get; init; }

    public string? SupportingDepartmentTitle { get; init; }

    // فقط برای سطح employee (کارمند بانک؛ در سطح‌های دیگر null): نام و نام خانوادگی جدا (title همان نام کامل و code همان کد پرسنلی است).
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    // فقط برای سطح employee: ساختار سازمانی (جایگاه شغلی) کارمند.
    public OrganizationStructureReferenceDto? OrganizationStructure { get; init; }

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
