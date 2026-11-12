using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.TechnicianAssignment;
using EMS.Application.Dtos.TechnicianAssignmentStatus;
using EMS.Application.Interfaces.TechnicianAssignment;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    /// <summary>
    /// ارجاع گزارش خرابی به تکنسین. Assigning and cancelling need <c>TechnicianAssignments.Assign</c>; accept / reject / start / complete and
    /// recording actions need <c>TechnicianAssignments.Work</c> AND the signed-in user must be the assignment's technician (403 otherwise).
    /// </summary>
    public class TechnicianAssignmentController : BaseApiController
    {
        private readonly ITechnicianAssignmentService _service;

        public TechnicianAssignmentController(ITechnicianAssignmentService service)
        {
            _service = service;
        }

        // ------------------------------------------------------------------------------------------ read

        [Permission(Permissions.TechnicianAssignments.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianAssignmentListItemDto>>>> GetAllList(
            [FromQuery] TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.TechnicianAssignments.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        /// <summary>The statuses an assignment can have (a fixed list).</summary>
        [Lookup]
        [HttpGet("statuses")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianAssignmentStatusItemDto>>>> GetStatuses(CancellationToken cancellationToken) =>
            Success(await _service.GetStatusesAsync(cancellationToken));

        /// <summary>The assignments of a report, newest filters and sorting as in the list. 404 for an unknown report.</summary>
        [Permission(Permissions.TechnicianAssignments.View)]
        [HttpGet("~/api/FailureReport/{failureReportId:long}/assignments")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianAssignmentListItemDto>>>> GetOfReport(
            long failureReportId, [FromQuery] TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetOfReportAsync(failureReportId, request, cancellationToken));

        /// <summary>The assignments of a user as technician ("my assignments" for the signed-in user). 404 for an unknown user.</summary>
        [Permission(Permissions.TechnicianAssignments.View)]
        [HttpGet("~/api/Users/{userId:long}/assignments")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianAssignmentListItemDto>>>> GetOfUser(
            long userId, [FromQuery] TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetOfUserAsync(userId, request, cancellationToken));

        [Permission(Permissions.TechnicianAssignments.View)]
        [HttpGet("{id:long}/actions")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianAssignmentActionItemDto>>>> GetActions(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetActionsAsync(id, cancellationToken));

        // ------------------------------------------------------------------------------------------ write

        /// <summary>Assigns the report to a technician (status Assigned). 409 when this technician already has an open assignment on it, or the report is closed.</summary>
        [Permission(Permissions.TechnicianAssignments.Assign)]
        [HttpPost("~/api/FailureReport/{failureReportId:long}/assignments")]
        [ProducesResponseType<ApiResponse<TechnicianAssignmentDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Assign(
            long failureReportId, [FromBody] TechnicianAssignmentCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.AssignAsync(failureReportId, request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Assigns the report to several technicians at once (one task each; up to 20). All or nothing. A report may have several open assignments, a technician at most one.
        /// </summary>
        [Permission(Permissions.TechnicianAssignments.Assign)]
        [HttpPost("~/api/FailureReport/{failureReportId:long}/assignments/bulk")]
        [ProducesResponseType<ApiResponse<IReadOnlyList<TechnicianAssignmentDetailDto>>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianAssignmentDetailDto>>>> AssignBulk(
            long failureReportId, [FromBody] TechnicianAssignmentBulkCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.AssignBulkAsync(failureReportId, request, cancellationToken);
            return CreatedAt(nameof(GetAllList), new { failureReportId }, result);
        }

        /// <summary>
        /// Edits an OPEN assignment: replaces title, priority, description and deadline (the technician and the status are not edited: cancel and assign again, or use the
        /// transitions). The time the technician saw it is kept. 409 <c>assignment_not_editable</c> for a finished one.
        /// </summary>
        [Permission(Permissions.TechnicianAssignments.Assign)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Update(long id, [FromBody] TechnicianAssignmentUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        /// <summary>Cancels an open assignment (the reason is required).</summary>
        [Permission(Permissions.TechnicianAssignments.Assign)]
        [HttpPost("{id:long}/cancel")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Cancel(long id, [FromBody] TechnicianAssignmentReasonDto request, CancellationToken cancellationToken) =>
            Updated(await _service.CancelAsync(id, request, cancellationToken));

        /// <summary>
        /// The technician saw the assignment (call it when he actually opens it). Only the FIRST time is recorded in <c>viewedDateTime</c>; calling again changes
        /// nothing. Only the assigned technician (403 for anyone else, even the one who assigned it). Accepting, rejecting, starting, completing or recording an
        /// action also records it if it was still empty.
        /// </summary>
        [Permission(Permissions.TechnicianAssignments.Work)]
        [HttpPost("{id:long}/seen")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> MarkSeen(long id, CancellationToken cancellationToken) =>
            Updated(await _service.MarkSeenAsync(id, cancellationToken));

        [Permission(Permissions.TechnicianAssignments.Work)]
        [HttpPost("{id:long}/accept")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Accept(long id, [FromBody] TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken) =>
            Updated(await _service.AcceptAsync(id, request, cancellationToken));

        /// <summary>The technician refuses the assignment (the reason is required).</summary>
        [Permission(Permissions.TechnicianAssignments.Work)]
        [HttpPost("{id:long}/reject")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Reject(long id, [FromBody] TechnicianAssignmentReasonDto request, CancellationToken cancellationToken) =>
            Updated(await _service.RejectAsync(id, request, cancellationToken));

        /// <summary>Starts the work; an Open report becomes "در حال رسیدگی".</summary>
        [Permission(Permissions.TechnicianAssignments.Work)]
        [HttpPost("{id:long}/start")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Start(long id, [FromBody] TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken) =>
            Updated(await _service.StartAsync(id, request, cancellationToken));

        /// <summary>Completes the work. The report is NOT closed by this.</summary>
        [Permission(Permissions.TechnicianAssignments.Work)]
        [HttpPost("{id:long}/complete")]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentDetailDto>>> Complete(long id, [FromBody] TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken) =>
            Updated(await _service.CompleteAsync(id, request, cancellationToken));

        /// <summary>Records an action performed during the work (only while the assignment is in progress).</summary>
        [Permission(Permissions.TechnicianAssignments.Work)]
        [HttpPost("{id:long}/actions")]
        [ProducesResponseType<ApiResponse<TechnicianAssignmentActionItemDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<TechnicianAssignmentActionItemDto>>> AddAction(long id, [FromBody] TechnicianAssignmentActionCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.AddActionAsync(id, request, cancellationToken);
            return CreatedAt(nameof(GetActions), new { id }, result);
        }
    }
}
