using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.AssignmentNote;
using EMS.Application.Interfaces.AssignmentNote;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

public sealed class AssignmentNoteController : BaseApiController
{
    private readonly IAssignmentNoteService _service;
    public AssignmentNoteController(IAssignmentNoteService service) => _service = service;

    [Permission(Permissions.AssignmentNotes.View)]
    [HttpGet("~/api/TechnicianAssignment/{assignmentId:long}/notes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AssignmentNoteItemDto>>>> GetOfAssignment(
        long assignmentId, [FromQuery] AssignmentNoteListRequestDto request, CancellationToken cancellationToken) =>
        Paged(await _service.GetPagedAsync(assignmentId, request, cancellationToken));

    [Permission(Permissions.AssignmentNotes.View)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<AssignmentNoteItemDto>>> GetById(long id, CancellationToken cancellationToken) =>
        Success(await _service.GetByIdAsync(id, cancellationToken));

    [Permission(Permissions.AssignmentNotes.Create)]
    [HttpPost("~/api/TechnicianAssignment/{assignmentId:long}/notes")]
    [ProducesResponseType<ApiResponse<AssignmentNoteItemDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AssignmentNoteItemDto>>> Create(
        long assignmentId, [FromBody] AssignmentNoteCreateDto request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(assignmentId, request, cancellationToken);
        return CreatedAt(nameof(GetById), new { id = result.Id }, result);
    }

    [Permission(Permissions.AssignmentNotes.Update)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<AssignmentNoteItemDto>>> Update(
        long id, [FromBody] AssignmentNoteUpdateDto request, CancellationToken cancellationToken) =>
        Updated(await _service.UpdateAsync(id, request, cancellationToken));

    [Permission(Permissions.AssignmentNotes.Delete)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, [FromQuery] string? rowVersion, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, rowVersion, cancellationToken);
        return Deleted();
    }

    [Permission(Permissions.AssignmentNotes.MarkSeen)]
    [HttpPost("{id:long}/seen")]
    public async Task<ActionResult<ApiResponse<AssignmentNoteItemDto>>> MarkSeen(
        long id, [FromBody] AssignmentNoteSeenDto request, CancellationToken cancellationToken) =>
        Updated(await _service.MarkSeenAsync(id, request, cancellationToken));
}
