using System.Globalization;
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
        var groupBy = Normalize(request.GroupBy);

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

                _ => query.Where(x => false)
            };
        }

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

        return groupBy.Length == 0
            ? await GetListAsync(
                query,
                level,
                request,
                cancellationToken)
            : await GetGroupsAsync(
                query,
                groupBy,
                request,
                cancellationToken);
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

            _ => throw new InvalidOperationException(
                "سطح جستجو پیش از اجرای Query باید اعتبارسنجی شود.")
        };
    }

    private static async Task<OrganizationHierarchyResult> GetListAsync(
        IQueryable<HierarchyRow> query,
        string level,
        OrganizationHierarchyRequestDto request,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var descending = Normalize(request.SortDirection) == "desc";

        var sortBy = Normalize(request.SortBy);

        IOrderedQueryable<HierarchyRow> ordered = sortBy switch
        {
            "id" => descending
                ? query.OrderByDescending(x => x.Id)
                : query.OrderBy(x => x.Id),

            "code" => descending
                ? query.OrderByDescending(x => x.Code).ThenBy(x => x.Id)
                : query.OrderBy(x => x.Code).ThenBy(x => x.Id),

            _ => descending
                ? query.OrderByDescending(x => x.Title).ThenBy(x => x.Id)
                : query.OrderBy(x => x.Title).ThenBy(x => x.Id)
        };

        var rows = await ordered
            .Skip(GetSkip(request))
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => MapItem(x, level))
            .ToList();

        return new OrganizationHierarchyResult
        {
            Data = new OrganizationHierarchyResponseDto
            {
                Mode = "list",
                Items = items,
                Groups = null
            },
            Pagination = CreatePagination(request, totalCount)
        };
    }

    private static async Task<OrganizationHierarchyResult> GetGroupsAsync(
        IQueryable<HierarchyRow> query,
        string groupBy,
        OrganizationHierarchyRequestDto request,
        CancellationToken cancellationToken)
    {
        var keys = BuildGroupKeys(query, groupBy);

        var groups = keys
            .GroupBy(x => new
            {
                x.Id,
                x.Level,
                x.Name,
                x.State
            })
            .Select(x => new GroupRow
            {
                Id = x.Key.Id,
                Level = x.Key.Level,
                Name = x.Key.Name,
                State = x.Key.State,
                Count = x.Count()
            });

        // تعداد گروه‌ها، نه تعداد اعضای گروه‌ها.
        var totalCount = await groups.CountAsync(cancellationToken);

        var descending = Normalize(request.SortDirection) == "desc";

        var sortBy = Normalize(request.SortBy);

        IOrderedQueryable<GroupRow> ordered = sortBy switch
        {
            "count" => descending
                ? groups.OrderByDescending(x => x.Count)
                : groups.OrderBy(x => x.Count),

            "key" when groupBy == "isactive" => descending
                ? groups.OrderByDescending(x => x.State)
                : groups.OrderBy(x => x.State),

            "key" => descending
                ? groups.OrderByDescending(x => x.Level)
                    .ThenByDescending(x => x.Id)
                : groups.OrderBy(x => x.Level)
                    .ThenBy(x => x.Id),

            _ => descending
                ? groups.OrderByDescending(x => x.Name)
                : groups.OrderBy(x => x.Name)
        };

        // ترتیب قطعی برای صفحه‌بندی پایدار.
        var rows = await ordered
            .ThenBy(x => x.Level)
            .ThenBy(x => x.Id)
            .ThenBy(x => x.State)
            .Skip(GetSkip(request))
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var result = rows.Select(x =>
            new OrganizationHierarchyGroupDto
            {
                Key = GetGroupKey(x, groupBy),
                Level = x.Level,
                Name = x.Name,
                Count = x.Count
            }).ToList();

        return new OrganizationHierarchyResult
        {
            Data = new OrganizationHierarchyResponseDto
            {
                Mode = "grouped",
                Items = null,
                Groups = result
            },
            Pagination = CreatePagination(request, totalCount)
        };
    }

    private static IQueryable<GroupKeyRow> BuildGroupKeys(
        IQueryable<HierarchyRow> query,
        string groupBy)
    {
        return groupBy switch
        {
            "zone" => query.Select(x => new GroupKeyRow
            {
                Id = x.ZoneId,
                Level = "zone",
                Name = x.ZoneTitle ?? "بدون ناحیه",
                State = null
            }),

            "organizationmanagement" => query.Select(x => new GroupKeyRow
            {
                Id = x.ManagementId,
                Level = "organizationManagement",
                Name = x.ManagementTitle ?? "بدون مدیریت شعب",
                State = null
            }),

            "field" => query.Select(x => new GroupKeyRow
            {
                Id = x.FieldId,
                Level = "field",
                Name = x.FieldTitle ?? "بدون حوزه",
                State = null
            }),

            "parent" => query.Select(x => new GroupKeyRow
            {
                Id = x.ParentId,
                Level = x.ParentLevel ?? "none",
                Name = x.ParentTitle ?? "بدون والد",
                State = null
            }),

            "isactive" => query.Select(x => new GroupKeyRow
            {
                Id = null,
                Level = "isActive",
                Name = x.IsActive ? "فعال" : "غیرفعال",
                State = x.IsActive
            }),

            _ => throw new InvalidOperationException(
                "گروه‌بندی باید پیش از اجرای Query اعتبارسنجی شود.")
        };
    }

    private static string? GetGroupKey(
        GroupRow row,
        string groupBy)
    {
        if (groupBy == "isactive")
        {
            return row.State == true ? "true" : "false";
        }

        if (!row.Id.HasValue)
        {
            return null;
        }

        var id = row.Id.Value.ToString(CultureInfo.InvariantCulture);

        // شناسه حوزه و مدیریت ممکن است یکسان باشد.
        // در گروه‌بندی والد، هویت گروه باید سطح‌دار باشد.
        return groupBy == "parent"
            ? $"{row.Level}:{id}"
            : id;
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
            Ancestors = ancestors
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

    private static int GetSkip(
        OrganizationHierarchyRequestDto request)
    {
        return checked(
            (int)(((long)request.PageNumber - 1) * request.PageSize));
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
    }

    private sealed class GroupKeyRow
    {
        public long? Id { get; set; }

        public string Level { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool? State { get; set; }
    }

    private sealed class GroupRow
    {
        public long? Id { get; set; }

        public string Level { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool? State { get; set; }

        public int Count { get; set; }
    }
}
