using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Skill;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.Skill;
using EMS.Application.Validators.Skill;
using EMS.Domain.Interfaces;
using FluentValidation;
using System.Linq.Expressions;
using SkillEntity = EMS.Domain.Entities.Skill;

namespace EMS.Application.Services.Skill;

public sealed class SkillService : ISkillService
{
    private readonly ISkillRepository _repository;

    private readonly CatalogCrud _crud;

    private readonly SkillCreateDtoValidator _createValidator = new();

    private readonly SkillUpdateDtoValidator _updateValidator = new();

    private readonly SkillListRequestDtoValidator _listValidator = new();

    public SkillService(
        ISkillRepository repository,
        IUnitOfWork unitOfWork,
        IAuditEventLogger audit)
    {
        _repository = repository;

        _crud = new CatalogCrud(
            repository,
            unitOfWork,
            audit);
    }

    public Task<SkillDetailDto> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveId(id);

        return _crud.GetByIdAsync(
            id,
            cancellationToken);
    }

    public async Task<SkillDetailDto> CreateAsync(
        SkillCreateDto request,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        return await _crud.CreateAsync(
            request,
            cancellationToken);
    }

    public async Task<SkillDetailDto> UpdateAsync(
        long id,
        SkillUpdateDto request,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveId(id);

        await _updateValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        return await _crud.UpdateAsync(
            id,
            request,
            cancellationToken);
    }

    public Task DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        EnsurePositiveId(id);

        return _crud.DeleteAsync(
            id,
            cancellationToken);
    }

    public async Task<PagedResult<SkillListItemDto>> GetPagedAsync(
        SkillListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.GroupBy))
        {
            throw new BadRequestException(
                "برای گروه‌بندی از /api/Skills/groups استفاده کنید.");
        }

        await _listValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var rows = BuildRows(request);

        var descending = IsDescending(request.SortDirection);

        IOrderedQueryable<SkillListItemDto> ordered =
            (NormalizeKey(request.SortBy), descending) switch
            {
                ("title", true) =>
                    rows.OrderByDescending(x => x.Title),

                ("title", false) =>
                    rows.OrderBy(x => x.Title),

                ("code", true) =>
                    rows.OrderByDescending(x => x.Code),

                ("code", false) =>
                    rows.OrderBy(x => x.Code),

                ("description", true) =>
                    rows.OrderByDescending(x => x.Description),

                ("description", false) =>
                    rows.OrderBy(x => x.Description),

                ("isactive", true) =>
                    rows.OrderByDescending(x => x.IsActive),

                ("isactive", false) =>
                    rows.OrderBy(x => x.IsActive),

                ("techniciancount", true) =>
                    rows.OrderByDescending(x => x.TechnicianCount),

                ("techniciancount", false) =>
                    rows.OrderBy(x => x.TechnicianCount),

                ("createddatetime", true) =>
                    rows.OrderByDescending(x => x.CreatedDateTime),

                ("createddatetime", false) =>
                    rows.OrderBy(x => x.CreatedDateTime),

                ("modifieddatetime", true) =>
                    rows.OrderByDescending(x => x.ModifiedDateTime),

                ("modifieddatetime", false) =>
                    rows.OrderBy(x => x.ModifiedDateTime),

                (_, true) =>
                    rows.OrderByDescending(x => x.Id),

                _ =>
                    rows.OrderBy(x => x.Id)
            };

        return await ordered
            .ThenBy(x => x.Id)
            .ToPagedResultAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);
    }

    public async Task<PagedResult<SkillGroupDto>> GetGroupedAsync(
        SkillListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.GroupBy))
        {
            throw new BadRequestException(
                "ارسال GroupBy برای گروه‌بندی الزامی است.");
        }

        await _listValidator.ValidateAndThrowAsync(
            request,
            cancellationToken);

        var rows = BuildRows(request);

        IQueryable<SkillGroupDto> groups;

        switch (NormalizeKey(request.GroupBy))
        {
            case "isactive":
                groups = rows
                    .GroupBy(x => x.IsActive)
                    .Select(g => new SkillGroupDto
                    {
                        Key = g.Key ? "true" : "false",
                        Name = g.Key ? "فعال" : "غیرفعال",
                        SkillCount = g.Count()
                    });
                break;

            case "code":
                groups = rows
                    .GroupBy(x => x.Code)
                    .Select(g => new SkillGroupDto
                    {
                        Key = g.Key,
                        Name = g.Key == null || g.Key == ""
                            ? "بدون کد"
                            : g.Key,
                        SkillCount = g.Count()
                    });
                break;

            default: // hasTechnician
                groups = rows
                    .GroupBy(x => x.TechnicianCount > 0)
                    .Select(g => new SkillGroupDto
                    {
                        Key = g.Key ? "true" : "false",
                        Name = g.Key
                            ? "دارای تکنسین"
                            : "بدون تکنسین",
                        SkillCount = g.Count()
                    });
                break;
        }

        var descending = IsDescending(request.SortDirection);

        IOrderedQueryable<SkillGroupDto> ordered =
            (NormalizeKey(request.SortBy), descending) switch
            {
                ("skillcount", true) =>
                    groups.OrderByDescending(x => x.SkillCount),

                ("skillcount", false) =>
                    groups.OrderBy(x => x.SkillCount),

                ("key", true) =>
                    groups.OrderByDescending(x => x.Key),

                ("key", false) =>
                    groups.OrderBy(x => x.Key),

                (_, true) =>
                    groups.OrderByDescending(x => x.Name),

                _ =>
                    groups.OrderBy(x => x.Name)
            };

        return await ordered
            .ThenBy(x => x.Key)
            .ToPagedResultAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);
    }

    private IQueryable<SkillListItemDto> BuildRows(
        SkillListRequestDto request)
    {
        var query = _repository.QueryForRead();

        // Explicit IsActive takes precedence over IncludeInactive.
        if (request.IsActive.HasValue)
        {
            var active = request.IsActive.Value;

            query = query.Where(x => x.IsActive == active);
        }
        else if (!request.IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (request.Id.HasValue)
        {
            var id = request.Id.Value;

            query = query.Where(x => x.SkillId == id);
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var title = request.Title.Trim();

            query = query.Where(x => x.Title.Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim();

            query = query.Where(x =>
                x.Code != null &&
                x.Code.Contains(code));
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            var description = request.Description.Trim();

            query = query.Where(x =>
                x.Description != null &&
                x.Description.Contains(description));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();

            var isNumeric = long.TryParse(
                search,
                out var numericId);

            query = query.Where(x =>
                x.Title.Contains(search) ||

                (x.Code != null &&
                 x.Code.Contains(search)) ||

                (x.Description != null &&
                 x.Description.Contains(search)) ||

                (isNumeric && x.SkillId == numericId));
        }

        if (request.TechnicianUserId.HasValue)
        {
            var technicianUserId = request.TechnicianUserId.Value;

            query = query.Where(x =>
                x.TechnicianSkills.Any(relation =>
                    relation.IsActive &&
                    relation.TechnicianUserId == technicianUserId));
        }

        if (request.CreatedFrom.HasValue)
        {
            var from = request.CreatedFrom.Value;

            query = query.Where(x =>
                x.CreatedDateTime >= from);
        }

        if (request.CreatedTo.HasValue)
        {
            var to = request.CreatedTo.Value;

            query = query.Where(x =>
                x.CreatedDateTime <= to);
        }

        if (request.ModifiedFrom.HasValue)
        {
            var from = request.ModifiedFrom.Value;

            query = query.Where(x =>
                x.ModifiedDateTime.HasValue &&
                x.ModifiedDateTime.Value >= from);
        }

        if (request.ModifiedTo.HasValue)
        {
            var to = request.ModifiedTo.Value;

            query = query.Where(x =>
                x.ModifiedDateTime.HasValue &&
                x.ModifiedDateTime.Value <= to);
        }

        var rows = query.Select(x => new SkillListItemDto
        {
            Id = x.SkillId,
            Title = x.Title,
            Code = x.Code,
            Description = x.Description,
            IsActive = x.IsActive,
            CreatedDateTime = x.CreatedDateTime,
            ModifiedDateTime = x.ModifiedDateTime,

            TechnicianCount = x.TechnicianSkills
                .Where(relation => relation.IsActive)
                .Select(relation => relation.TechnicianUserId)
                .Distinct()
                .Count()
        });

        if (request.HasTechnician.HasValue)
        {
            rows = request.HasTechnician.Value
                ? rows.Where(x => x.TechnicianCount > 0)
                : rows.Where(x => x.TechnicianCount == 0);
        }

        if (request.MinTechnicianCount.HasValue)
        {
            var minimum = request.MinTechnicianCount.Value;

            rows = rows.Where(x =>
                x.TechnicianCount >= minimum);
        }

        if (request.MaxTechnicianCount.HasValue)
        {
            var maximum = request.MaxTechnicianCount.Value;

            rows = rows.Where(x =>
                x.TechnicianCount <= maximum);
        }

        return rows;
    }

    private static void EnsurePositiveId(long id)
    {
        if (id <= 0)
        {
            throw new BadRequestException(
                "شناسه مهارت باید بزرگتر از صفر باشد.");
        }
    }

    private static string NormalizeKey(string? value)
    {
        return value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static bool IsDescending(string? value)
    {
        return NormalizeKey(value) is "desc" or "dsc";
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    // Reuse the existing generic CRUD without changing its public API.
    private sealed class CatalogCrud : CatalogService<
        SkillEntity,
        SkillCreateDto,
        SkillUpdateDto,
        SkillDetailDto,
        SkillListItemDto,
        SkillListRequestDto>
    {
        public CatalogCrud(
            ISkillRepository repository,
            IUnitOfWork unitOfWork,
            IAuditEventLogger audit)
            : base(repository, unitOfWork, audit)
        {
        }

        protected override string Label => "مهارت";

        protected override string DependentsMessage =>
            "این مهارت دارای انتساب فعال به تکنسین است و نمی‌توان آن را غیرفعال کرد.";

        protected override Expression<Func<SkillEntity, long>>
            IdSelector => x => x.SkillId;

        protected override Expression<Func<SkillEntity, SkillListItemDto>>
            ListItem => x => new SkillListItemDto
            {
                Id = x.SkillId,
                Title = x.Title,
                Code = x.Code,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedDateTime = x.CreatedDateTime,
                ModifiedDateTime = x.ModifiedDateTime,

                TechnicianCount = x.TechnicianSkills
                    .Where(relation => relation.IsActive)
                    .Select(relation => relation.TechnicianUserId)
                    .Distinct()
                    .Count()
            };

        protected override SkillDetailDto ToDetail(
            SkillEntity entity)
        {
            return new SkillDetailDto
            {
                Id = entity.SkillId,
                Title = entity.Title,
                Code = entity.Code,
                Description = entity.Description,
                IsActive = entity.IsActive,
                CreatedDateTime = entity.CreatedDateTime,
                ModifiedDateTime = entity.ModifiedDateTime,

                RowVersion = RowVersionToken.From(
                    entity.ModifiedDateTime)
            };
        }

        protected override SkillEntity NewEntity(
            SkillCreateDto request,
            string title)
        {
            return new SkillEntity
            {
                Title = title,
                Code = NormalizeOptional(request.Code),
                Description = NormalizeOptional(request.Description),
                IsActive = true
            };
        }

        protected override void ApplyUpdate(
            SkillEntity entity,
            SkillUpdateDto request)
        {
            if (request.Code is not null)
            {
                entity.Code = NormalizeOptional(request.Code);
            }

            if (request.Description is not null)
            {
                entity.Description = NormalizeOptional(
                    request.Description);
            }
        }
    }
}
