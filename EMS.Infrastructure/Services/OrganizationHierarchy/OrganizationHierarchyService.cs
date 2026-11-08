using EMS.Application.Common.Models;
using EMS.Application.Dtos.OrganizationHierarchy;
using EMS.Application.Interfaces.OrganizationHierarchy;
using EMS.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Services.OrganizationHierarchy;

public sealed class OrganizationHierarchyService
    : IOrganizationHierarchyService
{
    private readonly EMSDbContext _db;

    private readonly IValidator<OrganizationHierarchyRequestDto> _validator;

    public OrganizationHierarchyService(
        EMSDbContext db,
        IValidator<OrganizationHierarchyRequestDto> validator)
    {
        _db = db;
        _validator = validator;
    }

    public async Task<OrganizationHierarchyResult> GetAsync(
        OrganizationHierarchyRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var level = Normalize(request.SearchLevel);
        var parentLevel = Normalize(request.ParentLevel);

        var query = BuildQuery(level);

        // سطح والد بدون شناسه: پاسخ موفق و خالی.
        if (parentLevel.Length > 0 && !request.ParentId.HasValue)
        {
            query = query.Where(x => false);
        }
        else if (request.ParentId.HasValue)
        {
            var parentId = request.ParentId.Value;

            query = parentLevel switch
            {
                "zone" =>
                    query.Where(x => x.ZoneId == parentId),

                "organizationmanagement" =>
                    query.Where(x => x.ManagementId == parentId),

                "field" =>
                    query.Where(x => x.FieldId == parentId),

                // only the employee level has a branch to be filtered by (the validator refuses it for the other levels)
                "organization" =>
                    query.Where(x => x.OrganizationId == parentId),

                _ => query.Where(x => false)
            };
        }

        if (request.SupportingDepartmentId.HasValue)
        {
            var departmentId = request.SupportingDepartmentId.Value;

            query = query.Where(x => x.SupportingDepartmentId == departmentId);
        }

        if (request.WithoutSupportingDepartment == true)
        {
            query = query.Where(x => x.SupportingDepartmentId == null);
        }

        // وضعیت فقط روی رکورد سطح نتیجه اعمال می‌شود.
        if (request.IsActive.HasValue)
        {
            var isActive = request.IsActive.Value;

            query = query.Where(x => x.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            query = query.Where(x =>
                x.Title.Contains(search) ||
                (x.Code != null && x.Code.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var title = request.Title.Trim();

            query = query.Where(x => x.Title.Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim();

            query = query.Where(x => x.Code == code);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var skip = checked(
            (int)(((long)request.PageNumber - 1) * request.PageSize));

        // مرتب‌سازی ثابت و قطعی برای صفحه‌بندی (کارمند: نام خانوادگی، نام، شناسه).
        var ordered = level == "employee"
            ? query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id)
            : query.OrderBy(x => x.Title).ThenBy(x => x.Id);

        var rows = await ordered
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new OrganizationHierarchyResult
        {
            Data = new OrganizationHierarchyResponseDto
            {
                Items = rows
                    .Select(x => MapItem(x, level))
                    .ToList()
            },
            Pagination = CreatePagination(request, totalCount)
        };
    }

    private IQueryable<HierarchyRow> BuildQuery(string level)
    {
        return level switch
        {
            "zone" => _db.Zones
                .AsNoTracking()
                .Select(x => new HierarchyRow
                {
                    Id = x.ZoneId,
                    Title = x.Title,
                    Code = x.Code,
                    IsActive = x.IsActive
                }),

            "organizationmanagement" => _db.OrganizationManagements
                .AsNoTracking()
                .Select(x => new HierarchyRow
                {
                    Id = x.OrganizationManagementId,
                    Title = x.Title,
                    Code = x.Code,
                    IsActive = x.IsActive,

                    ZoneId = x.Zone.ZoneId,
                    ZoneTitle = x.Zone.Title,
                    ZoneCode = x.Zone.Code,

                    ParentId = x.Zone.ZoneId,
                    ParentLevel = "zone",
                    ParentTitle = x.Zone.Title,
                    ParentCode = x.Zone.Code
                }),

            "field" => _db.Fields
                .AsNoTracking()
                .Select(x => new HierarchyRow
                {
                    Id = x.FieldId,
                    Title = x.Title,
                    Code = x.Code,
                    IsActive = x.IsActive,

                    ZoneId = x.OrganizationManagement.Zone.ZoneId,
                    ZoneTitle = x.OrganizationManagement.Zone.Title,
                    ZoneCode = x.OrganizationManagement.Zone.Code,

                    ManagementId =
                        x.OrganizationManagement.OrganizationManagementId,

                    ManagementTitle = x.OrganizationManagement.Title,
                    ManagementCode = x.OrganizationManagement.Code,

                    ParentId =
                        x.OrganizationManagement.OrganizationManagementId,

                    ParentLevel = "organizationManagement",
                    ParentTitle = x.OrganizationManagement.Title,
                    ParentCode = x.OrganizationManagement.Code
                }),

            "organization" => _db.Organizations
                .AsNoTracking()
                .Select(x => new HierarchyRow
                {
                    Id = x.OrganizationId,
                    Title = x.Title,
                    Code = x.Code,
                    IsActive = x.IsActive,

                    SupportingDepartmentId = x.SupportingDepartmentId,
                    SupportingDepartmentTitle = x.SupportingDepartment!.Title,

                    ZoneId = x.FieldId.HasValue
                        ? x.Field!.OrganizationManagement.Zone.ZoneId
                        : x.OrganizationManagement!.Zone.ZoneId,

                    ZoneTitle = x.FieldId.HasValue
                        ? x.Field!.OrganizationManagement.Zone.Title
                        : x.OrganizationManagement!.Zone.Title,

                    ZoneCode = x.FieldId.HasValue
                        ? x.Field!.OrganizationManagement.Zone.Code
                        : x.OrganizationManagement!.Zone.Code,

                    ManagementId = x.FieldId.HasValue
                        ? x.Field!.OrganizationManagement.OrganizationManagementId
                        : x.OrganizationManagement!.OrganizationManagementId,

                    ManagementTitle = x.FieldId.HasValue
                        ? x.Field!.OrganizationManagement.Title
                        : x.OrganizationManagement!.Title,

                    ManagementCode = x.FieldId.HasValue
                        ? x.Field!.OrganizationManagement.Code
                        : x.OrganizationManagement!.Code,

                    FieldId = x.FieldId,

                    FieldTitle = x.FieldId.HasValue
                        ? x.Field!.Title
                        : null,

                    FieldCode = x.FieldId.HasValue
                        ? x.Field!.Code
                        : null,

                    ParentId = x.FieldId.HasValue
                        ? x.FieldId
                        : x.OrganizationManagementId,

                    ParentLevel = x.FieldId.HasValue
                        ? "field"
                        : "organizationManagement",

                    ParentTitle = x.FieldId.HasValue
                        ? x.Field!.Title
                        : x.OrganizationManagement!.Title,

                    ParentCode = x.FieldId.HasValue
                        ? x.Field!.Code
                        : x.OrganizationManagement!.Code
                }),

            // A bank employee sits directly under a branch (Employee.OrganizationId is required). Sensitive personal data (national code, birth
            // date) is deliberately not read here.
            "employee" => _db.Employees
                .AsNoTracking()
                .Select(x => new HierarchyRow
                {
                    Id = x.EmployeeId,
                    Title = x.FirstName + " " + x.LastName,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Code = x.PersonnelCode,
                    IsActive = x.IsActive,

                    OrganizationId = x.OrganizationId,

                    ZoneId = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.OrganizationManagement.Zone.ZoneId
                        : x.Organization.OrganizationManagement!.Zone.ZoneId,

                    ZoneTitle = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.OrganizationManagement.Zone.Title
                        : x.Organization.OrganizationManagement!.Zone.Title,

                    ZoneCode = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.OrganizationManagement.Zone.Code
                        : x.Organization.OrganizationManagement!.Zone.Code,

                    ManagementId = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.OrganizationManagement.OrganizationManagementId
                        : x.Organization.OrganizationManagement!.OrganizationManagementId,

                    ManagementTitle = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.OrganizationManagement.Title
                        : x.Organization.OrganizationManagement!.Title,

                    ManagementCode = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.OrganizationManagement.Code
                        : x.Organization.OrganizationManagement!.Code,

                    FieldId = x.Organization.FieldId,

                    FieldTitle = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.Title
                        : null,

                    FieldCode = x.Organization.FieldId.HasValue
                        ? x.Organization.Field!.Code
                        : null,

                    ParentId = x.OrganizationId,
                    ParentLevel = "organization",
                    ParentTitle = x.Organization.Title,
                    ParentCode = x.Organization.Code,

                    StructureId = x.OrganizationStructureId,
                    StructureCode = x.OrganizationStructure.OrganizationStructureCode,
                    StructureTitle = x.OrganizationStructure.Title,
                    StructureGrade = x.OrganizationStructure.Grade
                }),

            _ => throw new InvalidOperationException(
                "سطح جستجو پیش از اجرای Query باید اعتبارسنجی شود.")
        };
    }

    private static OrganizationHierarchyItemDto MapItem(
        HierarchyRow row,
        string level)
    {
        var ancestors = new List<OrganizationHierarchyReferenceDto>();

        if (row.ZoneId.HasValue)
        {
            ancestors.Add(CreateReference(
                row.ZoneId.Value,
                "zone",
                row.ZoneTitle,
                row.ZoneCode));
        }

        if (row.ManagementId.HasValue)
        {
            ancestors.Add(CreateReference(
                row.ManagementId.Value,
                "organizationManagement",
                row.ManagementTitle,
                row.ManagementCode));
        }

        if (row.FieldId.HasValue)
        {
            ancestors.Add(CreateReference(
                row.FieldId.Value,
                "field",
                row.FieldTitle,
                row.FieldCode));
        }

        OrganizationHierarchyReferenceDto? parent = null;

        if (row.ParentId.HasValue)
        {
            parent = CreateReference(
                row.ParentId.Value,
                row.ParentLevel!,
                row.ParentTitle,
                row.ParentCode);
        }

        return new OrganizationHierarchyItemDto
        {
            Id = row.Id,
            Level = level == "organizationmanagement"
                ? "organizationManagement"
                : level,
            Title = row.Title,
            Code = row.Code,
            IsActive = row.IsActive,
            Parent = parent,
            FirstName = row.FirstName,
            LastName = row.LastName,
            OrganizationStructure = row.StructureId.HasValue
                ? new OrganizationStructureReferenceDto
                {
                    Id = row.StructureId.Value,
                    Code = row.StructureCode,
                    Title = row.StructureTitle,
                    Grade = row.StructureGrade
                }
                : null,
            SupportingDepartmentId = row.SupportingDepartmentId,
            SupportingDepartmentTitle = row.SupportingDepartmentTitle,

            // والد مستقیم در Ancestors تکرار نمی‌شود.
            Ancestors = ancestors
                .Where(x =>
                    parent == null ||
                    x.Id != parent.Id ||
                    x.Level != parent.Level)
                .ToList()
        };
    }

    private static OrganizationHierarchyReferenceDto CreateReference(
        long id,
        string level,
        string? title,
        string? code)
    {
        return new OrganizationHierarchyReferenceDto
        {
            Id = id,
            Level = level,
            Title = title ?? string.Empty,
            Code = code
        };
    }

    private static ApiPagination CreatePagination(
        OrganizationHierarchyRequestDto request,
        int totalCount)
    {
        var totalPages = (int)Math.Ceiling(
            totalCount / (double)request.PageSize);

        return new ApiPagination
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = request.PageNumber > 1,
            HasNextPage = request.PageNumber < totalPages
        };
    }

    private static string Normalize(string? value)
    {
        return OrganizationHierarchyRules.Normalize(value);
    }

    private sealed class HierarchyRow
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Code { get; set; }

        public bool IsActive { get; set; }

        public long? SupportingDepartmentId { get; set; }

        public string? SupportingDepartmentTitle { get; set; }

        public long? ZoneId { get; set; }

        public string? ZoneTitle { get; set; }

        public string? ZoneCode { get; set; }

        public long? ManagementId { get; set; }

        public string? ManagementTitle { get; set; }

        public string? ManagementCode { get; set; }

        public long? FieldId { get; set; }

        public string? FieldTitle { get; set; }

        public string? FieldCode { get; set; }

        public long? ParentId { get; set; }

        public string? ParentLevel { get; set; }

        public string? ParentTitle { get; set; }

        public string? ParentCode { get; set; }

        // employee level only
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public long? OrganizationId { get; set; }

        public long? StructureId { get; set; }

        public string? StructureCode { get; set; }

        public string? StructureTitle { get; set; }

        public int? StructureGrade { get; set; }
    }
}
