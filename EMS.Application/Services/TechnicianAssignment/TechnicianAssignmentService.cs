using EMS.Application.Common.Catalog;
using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Filtering;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Common.People;
using EMS.Application.Dtos.Dashboard;
using EMS.Application.Dtos.TechnicianAssignment;
using EMS.Application.Dtos.TechnicianAssignmentStatus;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.TechnicianAssignment;
using EMS.Domain;
using Microsoft.EntityFrameworkCore;
using EMS.Domain.Entities;
using EMS.Domain.Enums;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using ActionEntityModel = EMS.Domain.Entities.ActionEntity;
using TechnicianAssignmentEntity = EMS.Domain.Entities.TechnicianAssignment;

namespace EMS.Application.Services.TechnicianAssignment
{
    /// <summary>
    /// Technician assignments (ارجاع به تکنسین). A failure report is assigned to a technician (any existing user); the technician accepts or
    /// rejects it, starts the work, records actions and completes it; the assigner may cancel it while it is open.
    /// <list type="bullet">
    /// <item>Status changes follow <see cref="TechnicianAssignmentCatalog.CanMove"/>; Rejected, Completed and Cancelled are final.</item>
    /// <item>A report may have several open assignments (assigned, accepted or in progress), one per technician; a technician gets a new one after his old one is final.</item>
    /// <item>Accept, reject, start, complete and recording actions are only for the assigned technician (403 otherwise); assigning and cancelling need only the permission.</item>
    /// <item>Starting an assignment moves an Open report to "در حال رسیدگی"; completing it does not close the report (closing stays a separate decision).</item>
    /// </list>
    /// Assignments are never deleted or deactivated: cancelling is the way to withdraw one, and every assignment stays as history.
    /// </summary>
    public sealed class TechnicianAssignmentService : ITechnicianAssignmentService
    {
        private static readonly FilterBuilder<TechnicianAssignmentRow, TechnicianAssignmentListRequestDto> Filters = new FilterBuilder<TechnicianAssignmentRow, TechnicianAssignmentListRequestDto>()
            .Search(r => r.Search, x => x.FailureReportReferenceNumber, x => x.EquipmentYaasCode, x => x.Description)
            .AnyOf(r => r.FailureReportIds, x => x.FailureReportId)
            .AnyOf(r => r.TechnicianUserIds, x => x.TechnicianUserId)
            .AnyOf(r => r.AssignedByUserIds, x => x.AssignedByUserId)
            .AnyOf(r => r.StatusIds, x => x.StatusId)
            .AtLeast(r => r.AssignedFrom, x => x.AssignedDateTime)
            .AtMost(r => r.AssignedTo, x => x.AssignedDateTime)
            .Add(r => r.Overdue == true ? OverdueNow() : null)
            .Add(r => r.Overdue == false ? NotOverdueNow() : null);

        private static System.Linq.Expressions.Expression<Func<TechnicianAssignmentRow, bool>> OverdueNow()
        {
            var now = DateTime.UtcNow;
            var open = TechnicianAssignmentCatalog.OpenIds;
            return x => x.DeadlineDateTime != null && x.DeadlineDateTime < now && open.Contains(x.StatusId);
        }

        private static System.Linq.Expressions.Expression<Func<TechnicianAssignmentRow, bool>> NotOverdueNow()
        {
            var now = DateTime.UtcNow;
            var open = TechnicianAssignmentCatalog.OpenIds;
            return x => x.DeadlineDateTime == null || x.DeadlineDateTime >= now || !open.Contains(x.StatusId);
        }

        private static readonly SortBuilder<TechnicianAssignmentRow> Sorting = new SortBuilder<TechnicianAssignmentRow>()
            .By("assigneddatetime", x => x.AssignedDateTime)
            .By("deadlinedatetime", x => x.DeadlineDateTime)
            .By("completeddatetime", x => x.CompletedDateTime)
            .By("status", x => x.StatusId)
            .By("reportreference", x => x.FailureReportReferenceNumber)
            .By("createddatetime", x => x.CreatedDateTime);

        private readonly ITechnicianAssignmentRepository _repository;
        private readonly ITechnicianAssignmentActionRepository _actions;
        private readonly ITechnicianAssignmentStatusRepository _statuses;
        private readonly IFailureReportRepository _reports;
        private readonly IActionEntityRepository _actionCatalog;
        private readonly IEquipmentLocationHistoryRepository _history;
        private readonly IUserReferenceRepository _people;
        private readonly ICurrentUserService _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;

        public TechnicianAssignmentService(
            ITechnicianAssignmentRepository repository,
            ITechnicianAssignmentActionRepository actions,
            ITechnicianAssignmentStatusRepository statuses,
            IFailureReportRepository reports,
            IActionEntityRepository actionCatalog,
            IEquipmentLocationHistoryRepository history,
            IUserReferenceRepository people,
            ICurrentUserService currentUser,
            IUnitOfWork unitOfWork,
            IAuditEventLogger audit)
        {
            _repository = repository;
            _actions = actions;
            _statuses = statuses;
            _reports = reports;
            _actionCatalog = actionCatalog;
            _history = history;
            _people = people;
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _audit = audit;
        }

        // ------------------------------------------------------------------------------------------------ read

        public Task<PagedResult<TechnicianAssignmentListItemDto>> GetPagedAsync(TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default) =>
            PageAsync(request, cancellationToken);

        public Task<TechnicianAssignmentDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            DetailAsync(id, cancellationToken);

        public async Task<PagedResult<TechnicianAssignmentListItemDto>> GetOfReportAsync(long failureReportId, TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!await _reports.ExistsIncludingInactiveAsync(x => x.FailureReportId == failureReportId, cancellationToken))
                throw ReportNotFound();

            request.FailureReportIds = new[] { failureReportId };
            return await PageAsync(request, cancellationToken);
        }

        public async Task<PagedResult<TechnicianAssignmentListItemDto>> GetOfUserAsync(long userId, TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            await RequireUserAsync(userId, cancellationToken);

            request.TechnicianUserIds = new[] { userId };
            return await PageAsync(request, cancellationToken);
        }

        public async Task<IReadOnlyList<TechnicianAssignmentActionItemDto>> GetActionsAsync(long id, CancellationToken cancellationToken = default)
        {
            if (!await _repository.ExistsIncludingInactiveAsync(x => x.TechnicianAssignmentId == id, cancellationToken))
                throw NotFound();

            return (await _repository.GetActionRowsAsync(id, cancellationToken)).Select(ToActionDto).ToList();
        }

        public async Task<IReadOnlyList<TechnicianAssignmentStatusItemDto>> GetStatusesAsync(CancellationToken cancellationToken = default) =>
            (await _statuses.GetAllAsync(cancellationToken))
                .OrderBy(x => x.TechnicianAssignmentStatusId)
                .Select(x => new TechnicianAssignmentStatusItemDto
                {
                    Id = x.TechnicianAssignmentStatusId,
                    Code = x.Code,
                    Title = x.Title,
                    IsOpen = TechnicianAssignmentCatalog.IsOpen(x.TechnicianAssignmentStatusId),
                })
                .ToList();

        public async Task<AssignmentDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
        {
            var rows = VisibleRows();
            var byStatus = (await rows.GroupBy(r => r.StatusId).Select(g => new { Id = g.Key, Count = g.Count() }).ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.Count);

            var now = DateTime.UtcNow;
            var openIds = TechnicianAssignmentCatalog.OpenIds;
            var overdue = await rows.CountAsync(r => r.DeadlineDateTime != null && r.DeadlineDateTime < now && openIds.Contains(r.StatusId), cancellationToken);

            return new AssignmentDashboardDto
            {
                Total = byStatus.Values.Sum(),
                OpenCount = byStatus.Where(x => openIds.Contains(x.Key)).Sum(x => x.Value),
                OverdueCount = overdue,
                ByStatus = (await _statuses.GetAllAsync(cancellationToken)).OrderBy(x => x.TechnicianAssignmentStatusId).Select(x => new AssignmentStatusCountDto
                {
                    Id = x.TechnicianAssignmentStatusId,
                    Code = x.Code,
                    Title = x.Title,
                    Count = byStatus.GetValueOrDefault(x.TechnicianAssignmentStatusId),
                    IsOpen = TechnicianAssignmentCatalog.IsOpen(x.TechnicianAssignmentStatusId),
                }).ToList(),
            };
        }

        // ------------------------------------------------------------------------------------------------ write

        public async Task<TechnicianAssignmentDetailDto> AssignAsync(long failureReportId, TechnicianAssignmentCreateDto request, CancellationToken cancellationToken = default)
        {
            var userId = RequireCurrentUser();
            await RequireAssignableReportAsync(failureReportId, cancellationToken);
            await RequireUserAsync(request.TechnicianUserId, cancellationToken);

            if ((await _repository.GetOpenTechnicianIdsOfReportAsync(failureReportId, cancellationToken)).Contains(request.TechnicianUserId))
                throw new ConflictException(AlreadyOpenMessage, ErrorCodes.AssignmentAlreadyOpen);

            var entity = NewAssignment(failureReportId, userId, request.TechnicianUserId, request.DeadlineDateTime, request.Description);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Created("TechnicianAssignment", entity.TechnicianAssignmentId);
            return await DetailAsync(entity.TechnicianAssignmentId, cancellationToken);
        }

        public async Task<IReadOnlyList<TechnicianAssignmentDetailDto>> AssignBulkAsync(long failureReportId, TechnicianAssignmentBulkCreateDto request, CancellationToken cancellationToken = default)
        {
            var userId = RequireCurrentUser();
            await RequireAssignableReportAsync(failureReportId, cancellationToken);

            // Every problem of every item is collected first: all or nothing, and the caller sees all of them at once.
            var people = await _people.GetPeopleAsync(request.Items.Select(i => (long?)i.TechnicianUserId), cancellationToken);
            var alreadyOpen = await _repository.GetOpenTechnicianIdsOfReportAsync(failureReportId, cancellationToken);
            var problems = new List<ApiErrorDetail>();
            for (var i = 0; i < request.Items.Count; i++)
            {
                var technician = request.Items[i].TechnicianUserId;
                if (!people.ContainsKey(technician))
                    problems.Add(new ApiErrorDetail { Field = $"items[{i}].technicianUserId", Message = "کاربر موردنظر پیدا نشد." });
                else if (alreadyOpen.Contains(technician))
                    problems.Add(new ApiErrorDetail { Field = $"items[{i}].technicianUserId", Message = AlreadyOpenMessage });
            }
            if (problems.Count > 0)
                throw new BulkItemsException(problems, $"ارجاع گروهی انجام نشد؛ {problems.Select(p => p.Field).Distinct().Count()} ردیف خطا دارد و هیچ ارجاعی ثبت نشد.");

            var entities = request.Items
                .Select(i => NewAssignment(failureReportId, userId, i.TechnicianUserId, i.DeadlineDateTime, i.Description))
                .ToList();
            await _repository.AddRangeAsync(entities, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);      // one save: the assignments are created together or not at all

            foreach (var entity in entities)
                _audit.Created("TechnicianAssignment", entity.TechnicianAssignmentId);

            var result = new List<TechnicianAssignmentDetailDto>();
            foreach (var entity in entities)
                result.Add(await DetailAsync(entity.TechnicianAssignmentId, cancellationToken));
            return result;
        }

        private const string AlreadyOpenMessage = "این تکنسین برای این گزارش خرابی یک ارجاع باز دارد؛ برای ارجاع دوباره، ابتدا آن را لغو کنید.";

        /// <summary>The report must exist, be active and not closed; it may already have open assignments of OTHER technicians.</summary>
        private async Task RequireAssignableReportAsync(long failureReportId, CancellationToken cancellationToken)
        {
            var report = await _reports.GetForUpdateAsync(failureReportId, cancellationToken) ?? throw ReportNotFound();

            if (!report.IsActive)
                throw new BadRequestException("گزارش خرابی غیرفعال است و ارجاع ندارد.", ErrorCodes.ParentInactive);
            if (report.FailureReportStatusId == FailureReportStatusCatalog.IdOf(FailureReportStatusCode.Closed))
                throw new ConflictException("گزارش خرابی بسته شده است و نمی‌توان آن را ارجاع داد.", ErrorCodes.FailureReportClosed);
        }

        private static TechnicianAssignmentEntity NewAssignment(long failureReportId, long assignedByUserId, long technicianUserId, DateTime? deadline, string? description) => new()
        {
            FailureReportId = failureReportId,
            TechnicianAssignmentStatusId = TechnicianAssignmentCatalog.IdOf(TechnicianAssignmentStatusCode.Assigned),
            TechnicianUserId = technicianUserId,
            AssignedByUserId = assignedByUserId,
            AssignedDateTime = DateTime.UtcNow,
            DeadlineDateTime = deadline.HasValue ? ToUtc(deadline.Value) : null,
            Description = Normalize(description),
        };

        public Task<TechnicianAssignmentDetailDto> AcceptAsync(long id, TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken = default) =>
            MoveAsync(id, TechnicianAssignmentStatusCode.Accepted, request, technicianOnly: true, cancellationToken);

        public Task<TechnicianAssignmentDetailDto> RejectAsync(long id, TechnicianAssignmentReasonDto request, CancellationToken cancellationToken = default) =>
            MoveAsync(id, TechnicianAssignmentStatusCode.Rejected, request, technicianOnly: true, cancellationToken);

        public Task<TechnicianAssignmentDetailDto> StartAsync(long id, TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken = default) =>
            MoveAsync(id, TechnicianAssignmentStatusCode.InProgress, request, technicianOnly: true, cancellationToken);

        public Task<TechnicianAssignmentDetailDto> CompleteAsync(long id, TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken = default) =>
            MoveAsync(id, TechnicianAssignmentStatusCode.Completed, request, technicianOnly: true, cancellationToken);

        public Task<TechnicianAssignmentDetailDto> CancelAsync(long id, TechnicianAssignmentReasonDto request, CancellationToken cancellationToken = default) =>
            MoveAsync(id, TechnicianAssignmentStatusCode.Cancelled, request, technicianOnly: false, cancellationToken);

        public async Task<TechnicianAssignmentActionItemDto> AddActionAsync(long id, TechnicianAssignmentActionCreateDto request, CancellationToken cancellationToken = default)
        {
            var userId = RequireCurrentUser();
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();
            RequireTechnician(entity, userId);

            if (entity.TechnicianAssignmentStatusId != TechnicianAssignmentCatalog.IdOf(TechnicianAssignmentStatusCode.InProgress))
                throw new ConflictException("اقدام فقط برای ارجاعِ «در حال انجام» ثبت می‌شود.", ErrorCodes.AssignmentNotInProgress);

            await ParentGuard.RequireAsync<ActionEntityModel>(_actionCatalog, request.ActionId, x => x.ActionId, "اقدام", mustBeActive: true, cancellationToken);

            if (request.EquipmentLocationHistoryId is { } historyId)
            {
                var equipmentId = (await _reports.GetRowAsync(entity.FailureReportId, cancellationToken))?.EquipmentId;
                var history = await _history.FindAsync(x => x.EquipmentLocationHistoryId == historyId, asNoTracking: true, cancellationToken);
                if (history.Count == 0)
                    throw new NotFoundException("ردیف تاریخچه‌ی مکان تجهیز پیدا نشد.", ErrorCodes.ParentNotFound);
                if (history[0].EquipmentId != equipmentId)
                    throw new BadRequestException("ردیف تاریخچه‌ی مکان متعلق به تجهیز این گزارش نیست.", ErrorCodes.BadRequest);
            }

            var action = new TechnicianAssignmentAction
            {
                TechnicianAssignmentId = id,
                ActionId = request.ActionId,
                EquipmentLocationHistoryId = request.EquipmentLocationHistoryId,
                ActionDateTime = request.ActionDateTime.HasValue ? ToUtc(request.ActionDateTime.Value) : DateTime.UtcNow,
                Description = Normalize(request.Description),
            };
            await _actions.AddAsync(action, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Created("TechnicianAssignmentAction", action.TechnicianAssignmentActionId);
            var rows = await _repository.GetActionRowsAsync(id, cancellationToken);
            return ToActionDto(rows.First(r => r.Id == action.TechnicianAssignmentActionId));
        }

        // ------------------------------------------------------------------------------------------------ helpers

        private async Task<TechnicianAssignmentDetailDto> MoveAsync(
            long id, TechnicianAssignmentStatusCode target, TechnicianAssignmentTransitionDto request, bool technicianOnly, CancellationToken cancellationToken)
        {
            var userId = RequireCurrentUser();
            var entity = await _repository.GetForUpdateAsync(id, cancellationToken) ?? throw NotFound();
            if (technicianOnly)
                RequireTechnician(entity, userId);

            var from = (TechnicianAssignmentStatusCode)entity.TechnicianAssignmentStatusId;
            if (!TechnicianAssignmentCatalog.CanMove(from, target))
                throw new ConflictException(
                    $"ارجاعِ «{TechnicianAssignmentCatalog.TitleOf(from)}» را نمی‌توان به «{TechnicianAssignmentCatalog.TitleOf(target)}» تغییر داد.",
                    ErrorCodes.AssignmentTransitionNotAllowed);

            entity.TechnicianAssignmentStatusId = TechnicianAssignmentCatalog.IdOf(target);
            if (target == TechnicianAssignmentStatusCode.Completed)
                entity.CompletedDateTime = DateTime.UtcNow;
            var note = Normalize(request.Note);
            if (note is not null)
                entity.ResultNote = note;

            if (RowVersionToken.TryParse(request.RowVersion, out var expected))
                _repository.SetExpectedModifiedDateTime(entity, expected);
            _repository.Update(entity);

            // Starting the work moves an Open report to "در حال رسیدگی"; any other state of the report is left as it is.
            if (target == TechnicianAssignmentStatusCode.InProgress)
            {
                var report = await _reports.GetForUpdateAsync(entity.FailureReportId, cancellationToken);
                if (report is not null && report.FailureReportStatusId == FailureReportStatusCatalog.IdOf(FailureReportStatusCode.Open))
                {
                    report.FailureReportStatusId = FailureReportStatusCatalog.IdOf(FailureReportStatusCode.InProgress);
                    _reports.Update(report);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _audit.Operation(target.ToString(), "TechnicianAssignment", id);
            return await DetailAsync(id, cancellationToken);
        }

        /// <summary>
        /// THE source of assignment rows for the lists AND the dashboard counts (a future row-level security rule goes here, once;
        /// an assignment is visible when its failure report is).
        /// </summary>
        private IQueryable<TechnicianAssignmentRow> VisibleRows() => _repository.QueryRows();

        private async Task<PagedResult<TechnicianAssignmentListItemDto>> PageAsync(TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken)
        {
            var rows = Filters.Apply(VisibleRows(), request);
            var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            if (!Sorting.TryApply(request.SortBy, descending, rows, out var sorted))
                sorted = descending ? rows.OrderByDescending(x => x.Id) : rows.OrderBy(x => x.Id);

            var page = await sorted.ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);

            var people = await _people.GetPeopleAsync(page.Items.SelectMany(r => new long?[] { r.TechnicianUserId, r.AssignedByUserId }), cancellationToken);
            var items = page.Items.Select(row => Fill(new TechnicianAssignmentListItemDto(), row, people)).ToList();

            return new PagedResult<TechnicianAssignmentListItemDto>
            {
                Items = items,
                PageNumber = page.PageNumber,
                PageSize = page.PageSize,
                TotalCount = page.TotalCount,
            };
        }

        private async Task<TechnicianAssignmentDetailDto> DetailAsync(long id, CancellationToken cancellationToken)
        {
            var row = await _repository.GetRowAsync(id, cancellationToken) ?? throw NotFound();
            var people = await _people.GetPeopleAsync(new long?[] { row.TechnicianUserId, row.AssignedByUserId }, cancellationToken);
            var detail = Fill(new TechnicianAssignmentDetailDto(), row, people);
            detail.Actions = (await _repository.GetActionRowsAsync(id, cancellationToken)).Select(ToActionDto).ToList();
            detail.ModifiedDateTime = row.ModifiedDateTime;
            detail.RowVersion = RowVersionToken.From(row.ModifiedDateTime);
            return detail;
        }

        private static TDto Fill<TDto>(TDto dto, TechnicianAssignmentRow row, Dictionary<long, PersonReference> people) where TDto : TechnicianAssignmentListItemDto
        {
            dto.Id = row.Id;
            dto.FailureReportId = row.FailureReportId;
            dto.FailureReportReferenceNumber = row.FailureReportReferenceNumber;
            dto.EquipmentId = row.EquipmentId;
            dto.EquipmentYaasCode = row.EquipmentYaasCode;
            dto.StatusId = row.StatusId;
            dto.StatusCode = row.StatusCode;
            dto.StatusTitle = row.StatusTitle;
            dto.TechnicianUserId = row.TechnicianUserId;
            dto.AssignedByUserId = row.AssignedByUserId;
            dto.AssignedDateTime = row.AssignedDateTime;
            dto.DeadlineDateTime = row.DeadlineDateTime;
            dto.CompletedDateTime = row.CompletedDateTime;
            dto.IsOverdue = row.DeadlineDateTime is { } deadline && deadline < DateTime.UtcNow && TechnicianAssignmentCatalog.IsOpen(row.StatusId);
            dto.Description = row.Description;
            dto.ResultNote = row.ResultNote;
            dto.ActionCount = row.ActionCount;
            dto.CreatedDateTime = row.CreatedDateTime;
            PersonReferenceMapper.Fill(dto, "Technician", people.GetValueOrDefault(row.TechnicianUserId));
            PersonReferenceMapper.Fill(dto, "AssignedBy", people.GetValueOrDefault(row.AssignedByUserId));
            return dto;
        }

        private static TechnicianAssignmentActionItemDto ToActionDto(TechnicianAssignmentActionRow row) => new()
        {
            Id = row.Id,
            TechnicianAssignmentId = row.TechnicianAssignmentId,
            ActionId = row.ActionId,
            ActionTitle = row.ActionTitle,
            ActionCode = row.ActionCode,
            EquipmentLocationHistoryId = row.EquipmentLocationHistoryId,
            ActionDateTime = row.ActionDateTime,
            Description = row.Description,
            RecordedByUserId = row.CreatedByUserId,
        };

        private long RequireCurrentUser() =>
            _currentUser.UserId ?? throw new UnauthorizedAppException("احراز هویت انجام نشده یا توکن نامعتبر است.");

        private static void RequireTechnician(TechnicianAssignmentEntity entity, long userId)
        {
            if (entity.TechnicianUserId != userId)
                throw new ForbiddenException("این کار فقط برای تکنسینِ همین ارجاع مجاز است.");
        }

        private async Task RequireUserAsync(long userId, CancellationToken cancellationToken)
        {
            // active or not, the user only has to exist (like the technician of an equipment)
            if (!(await _people.GetPeopleAsync(new long?[] { userId }, cancellationToken)).ContainsKey(userId))
                throw new NotFoundException("کاربر موردنظر پیدا نشد.", ErrorCodes.UserNotFound);
        }

        private static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static NotFoundException NotFound() => new("ارجاع موردنظر پیدا نشد.", ErrorCodes.NotFound);

        private static NotFoundException ReportNotFound() => new("گزارش خرابی موردنظر پیدا نشد.", ErrorCodes.NotFound);
    }
}
