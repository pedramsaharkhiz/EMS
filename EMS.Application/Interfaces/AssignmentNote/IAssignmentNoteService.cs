using EMS.Application.Common.Models;
using EMS.Application.Dtos.AssignmentNote;

namespace EMS.Application.Interfaces.AssignmentNote;

public interface IAssignmentNoteService
{
    Task<PagedResult<AssignmentNoteItemDto>> GetPagedAsync(long assignmentId, AssignmentNoteListRequestDto request, CancellationToken cancellationToken);
    Task<AssignmentNoteItemDto> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<AssignmentNoteItemDto> CreateAsync(long assignmentId, AssignmentNoteCreateDto request, CancellationToken cancellationToken);
    Task<AssignmentNoteItemDto> UpdateAsync(long id, AssignmentNoteUpdateDto request, CancellationToken cancellationToken);
    Task DeleteAsync(long id, string? rowVersion, CancellationToken cancellationToken);
    Task<AssignmentNoteItemDto> MarkSeenAsync(long id, AssignmentNoteSeenDto request, CancellationToken cancellationToken);
}
