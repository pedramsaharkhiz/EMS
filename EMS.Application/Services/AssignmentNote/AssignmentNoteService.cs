using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Interfaces;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Common.People;
using EMS.Application.Dtos.AssignmentNote;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.AssignmentNote;
using EMS.Application.Validators.AssignmentNote;
using EMS.Domain.Entities;
using AssignmentEntity = EMS.Domain.Entities.TechnicianAssignment;
using EMS.Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Note = EMS.Domain.Entities.AssignmentNote;

namespace EMS.Application.Services.AssignmentNote;

public sealed class AssignmentNoteService : IAssignmentNoteService
{
    private readonly IAssignmentNoteRepository _notes;
    private readonly ITechnicianAssignmentRepository _assignments;
    private readonly INoteCategoryRepository _categories;
    private readonly IAssignmentNoteAccessPolicy _access;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditEventLogger _audit;
    private readonly IUserReferenceRepository _people;

    public AssignmentNoteService(IAssignmentNoteRepository notes, ITechnicianAssignmentRepository assignments,
        INoteCategoryRepository categories, IAssignmentNoteAccessPolicy access, ICurrentUserService user,
        IUnitOfWork unitOfWork, IAuditEventLogger audit, IUserReferenceRepository people)
    {
        _notes = notes; _assignments = assignments; _categories = categories; _access = access;
        _user = user; _unitOfWork = unitOfWork; _audit = audit; _people = people;
    }

    private long UserId => _user.UserId ?? throw new UnauthorizedAppException("ورود به سامانه الزامی است.");
    private static void Positive(long id, string field = "id")
    {
        if (id <= 0) throw new ValidationException(new[] { new ValidationFailure(field, "شناسه باید بزرگتر از صفر باشد.") });
    }
    private static void Allowed(bool allowed)
    {
        if (!allowed) throw new ForbiddenException("اجازه انجام این عملیات روی یادداشت این ارجاع را ندارید.");
    }
    private async Task<AssignmentEntity> AssignmentAsync(long id, CancellationToken ct)
    {
        Positive(id, "assignmentId");
        var assignment = await _assignments.Query().FirstOrDefaultAsync(x => x.TechnicianAssignmentId == id, ct)
            ?? throw new NotFoundException("ارجاع یافت نشد.");
        Allowed(_access.CanRead(assignment, UserId));
        return assignment;
    }
    private async Task<Note> NoteAsync(long id, bool tracked, CancellationToken ct)
    {
        Positive(id);
        var note = await _notes.GetAsync(id, tracked, ct) ?? throw new NotFoundException("یادداشت یافت نشد.");
        Allowed(_access.CanRead(note.TechnicianAssignment, UserId));
        return note;
    }
    private async Task CategoryAsync(long id, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("موضوع یادداشت یافت نشد.", ErrorCodes.ParentNotFound);
        if (!category.IsActive) throw new BadRequestException("موضوع یادداشت غیرفعال است.", ErrorCodes.ParentInactive);
    }
    private void ExpectVersion(Note note, string? version)
    {
        if (RowVersionToken.TryParse(version, out var expected)) _notes.SetExpectedModifiedDateTime(note, expected);
    }

    // Embedding notes must not expose them to other users who can view the assignment.
    public async Task<List<AssignmentNoteItemDto>> GetForAssignmentDetailAsync(long assignmentId, CancellationToken cancellationToken)
    {
        if (!_user.Permissions.Contains(EMS.Application.Common.Security.Permissions.AssignmentNotes.View))
            return [];
        var assignment = await _assignments.Query()
            .FirstOrDefaultAsync(x => x.TechnicianAssignmentId == assignmentId, cancellationToken);
        if (assignment is null || !_access.CanRead(assignment, UserId))
            return [];
        var notes = await _notes.QueryOfAssignment(assignmentId, true)
            .OrderBy(x => x.AssignmentNoteId).ToListAsync(cancellationToken);
        return await ItemsAsync(notes, cancellationToken);
    }

    public async Task<PagedResult<AssignmentNoteItemDto>> GetPagedAsync(long assignmentId, AssignmentNoteListRequestDto request, CancellationToken cancellationToken)
    {
        await AssignmentAsync(assignmentId, cancellationToken);
        await new AssignmentNoteListRequestDtoValidator().ValidateAndThrowAsync(request, cancellationToken);
        var query = _notes.QueryOfAssignment(assignmentId, request.IsActive ?? true);
        if (request.NoteCategoryId is { } category) query = query.Where(x => x.NoteCategoryId == category);
        if (request.IsSeen is { } seen) query = query.Where(x => x.SeenDateTime.HasValue == seen);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.Text.Contains(search));
        }
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Note> ordered = (request.SortBy?.ToLowerInvariant(), descending) switch
        {
            ("createddatetime", true) => query.OrderByDescending(x => x.CreatedDateTime),
            ("createddatetime", false) => query.OrderBy(x => x.CreatedDateTime),
            ("modifieddatetime", true) => query.OrderByDescending(x => x.ModifiedDateTime),
            ("modifieddatetime", false) => query.OrderBy(x => x.ModifiedDateTime),
            ("seendatetime", true) => query.OrderByDescending(x => x.SeenDateTime),
            ("seendatetime", false) => query.OrderBy(x => x.SeenDateTime),
            (_, true) => query.OrderByDescending(x => x.AssignmentNoteId),
            _ => query.OrderBy(x => x.AssignmentNoteId)
        };
        var page = await ordered.ThenBy(x => x.AssignmentNoteId).ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        return new PagedResult<AssignmentNoteItemDto>
        {
            Items = await ItemsAsync(page.Items, cancellationToken), PageNumber = page.PageNumber,
            PageSize = page.PageSize, TotalCount = page.TotalCount
        };
    }

    public async Task<AssignmentNoteItemDto> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        (await ItemsAsync(new[] { await NoteAsync(id, false, cancellationToken) }, cancellationToken)).Single();

    public async Task<AssignmentNoteItemDto> CreateAsync(long assignmentId, AssignmentNoteCreateDto request, CancellationToken cancellationToken)
    {
        var assignment = await AssignmentAsync(assignmentId, cancellationToken);
        Allowed(_access.CanCreate(assignment, UserId));
        await new AssignmentNoteCreateDtoValidator().ValidateAndThrowAsync(request, cancellationToken);
        await CategoryAsync(request.NoteCategoryId, cancellationToken);
        var note = new Note { TechnicianAssignmentId = assignmentId, NoteCategoryId = request.NoteCategoryId, Text = request.Text.Trim() };
        // SeenDateTime starts null even if the assignment's sender and technician are the same user.
        await _notes.AddAsync(note, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Created("AssignmentNote", note.AssignmentNoteId);
        return await GetByIdAsync(note.AssignmentNoteId, cancellationToken);
    }

    public async Task<AssignmentNoteItemDto> UpdateAsync(long id, AssignmentNoteUpdateDto request, CancellationToken cancellationToken)
    {
        var note = await NoteAsync(id, true, cancellationToken);
        Allowed(_access.CanEdit(note, UserId));
        if (!note.IsActive) throw new NotFoundException("یادداشت یافت نشد.");
        await new AssignmentNoteUpdateDtoValidator().ValidateAndThrowAsync(request, cancellationToken);
        if (note.NoteCategoryId != request.NoteCategoryId) await CategoryAsync(request.NoteCategoryId, cancellationToken);
        var text = request.Text.Trim();
        if (note.Text == text && note.NoteCategoryId == request.NoteCategoryId) return await GetByIdAsync(id, cancellationToken);
        ExpectVersion(note, request.RowVersion);
        note.Text = text;
        note.NoteCategoryId = request.NoteCategoryId;
        note.SeenDateTime = null;
        _notes.Update(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Updated("AssignmentNote", id);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(long id, string? rowVersion, CancellationToken cancellationToken)
    {
        var note = await NoteAsync(id, true, cancellationToken);
        Allowed(_access.CanEdit(note, UserId));
        if (!note.IsActive) return;
        ExpectVersion(note, rowVersion);
        note.IsActive = false;
        _notes.Update(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Deleted("AssignmentNote", id);
    }

    public async Task<AssignmentNoteItemDto> MarkSeenAsync(long id, AssignmentNoteSeenDto request, CancellationToken cancellationToken)
    {
        var note = await NoteAsync(id, true, cancellationToken);
        Allowed(_access.CanMarkSeen(note.TechnicianAssignment, UserId));
        if (!note.IsActive) throw new NotFoundException("یادداشت یافت نشد.");
        if (note.SeenDateTime.HasValue) return await GetByIdAsync(id, cancellationToken);
        // The optional version prevents acknowledging content changed since the technician read it.
        ExpectVersion(note, request.RowVersion);
        note.SeenDateTime = DateTime.UtcNow;
        _notes.Update(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _audit.Operation("Seen", "AssignmentNote", id);
        return await GetByIdAsync(id, cancellationToken);
    }

    private async Task<List<AssignmentNoteItemDto>> ItemsAsync(IEnumerable<Note> source, CancellationToken ct)
    {
        var notes = source.ToList();
        var people = await _people.GetPeopleAsync(notes.Select(x => x.CreatedByUserId), ct);
        return notes.Select(n =>
        {
            var item = new AssignmentNoteItemDto
            {
                Id = n.AssignmentNoteId, TechnicianAssignmentId = n.TechnicianAssignmentId,
                NoteCategoryId = n.NoteCategoryId, NoteCategoryTitle = n.NoteCategory.Title, Text = n.Text,
                SeenDateTime = n.SeenDateTime, IsActive = n.IsActive, CreatedByUserId = n.CreatedByUserId,
                CreatedDateTime = n.CreatedDateTime, ModifiedDateTime = n.ModifiedDateTime,
                RowVersion = RowVersionToken.From(n.ModifiedDateTime)
            };
            PersonReferenceMapper.Fill(item, "CreatedBy", n.CreatedByUserId is { } id ? people.GetValueOrDefault(id) : null);
            return item;
        }).ToList();
    }
}
