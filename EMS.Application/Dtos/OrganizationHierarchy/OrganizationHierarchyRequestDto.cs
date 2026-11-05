namespace EMS.Application.Dtos.OrganizationHierarchy;

public sealed class OrganizationHierarchyRequestDto
{
    public string SearchLevel { get; set; } = string.Empty;

    public string? ParentLevel { get; set; }

    public long? ParentId { get; set; }

    public string? Search { get; set; }

    public string? Title { get; set; }

    public string? Code { get; set; }

    public bool? IsActive { get; set; }

    // فقط برای سطح organization: فقط شعبه‌های زیر این دپارتمان پشتیبانی.
    public long? SupportingDepartmentId { get; set; }

    // فقط برای سطح organization: true = فقط شعبه‌های بدون دپارتمان پشتیبانی.
    public bool? WithoutSupportingDepartment { get; set; }

    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}
