using EMS.Application.Common.Models;
using EMS.Application.Dtos.TechnicianAssignment;
using EMS.Application.Dtos.TechnicianAssignmentStatus;

namespace EMS.Application.Interfaces.TechnicianAssignment
{
    public interface ITechnicianAssignmentService
    {
        Task<PagedResult<TechnicianAssignmentListItemDto>> GetPagedAsync(TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default);
        /// <summary>Dashboard: the assignments the caller sees, counted per status, with the open and the overdue ones.</summary>
        Task<EMS.Application.Dtos.Dashboard.AssignmentDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

        Task<TechnicianAssignmentDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>The assignments of one report (its history); 404 for an unknown report.</summary>
        Task<PagedResult<TechnicianAssignmentListItemDto>> GetOfReportAsync(long failureReportId, TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>The assignments of one user as technician; 404 for an unknown user.</summary>
        Task<PagedResult<TechnicianAssignmentListItemDto>> GetOfUserAsync(long userId, TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default);

        Task<TechnicianAssignmentDetailDto> AssignAsync(long failureReportId, TechnicianAssignmentCreateDto request, CancellationToken cancellationToken = default);

        /// <summary>Assigns the report to several technicians at once, each with his own task. All or nothing (400 <c>bulk_items_invalid</c> lists every refused item).</summary>
        Task<IReadOnlyList<TechnicianAssignmentDetailDto>> AssignBulkAsync(long failureReportId, TechnicianAssignmentBulkCreateDto request, CancellationToken cancellationToken = default);

        /// <summary>Edits title, priority, description and deadline of an OPEN assignment (409 <c>assignment_not_editable</c> once it is final).</summary>
        Task<TechnicianAssignmentDetailDto> UpdateAsync(long id, TechnicianAssignmentUpdateDto request, CancellationToken cancellationToken = default);

        /// <summary>Records that the technician saw the assignment (only the first time counts; only the technician himself; idempotent).</summary>
        Task<TechnicianAssignmentDetailDto> MarkSeenAsync(long id, CancellationToken cancellationToken = default);

        // transitions: Assigned -> Accepted | Rejected | Cancelled; Accepted -> InProgress | Cancelled; InProgress -> Completed | Cancelled
        Task<TechnicianAssignmentDetailDto> AcceptAsync(long id, TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken = default);
        Task<TechnicianAssignmentDetailDto> RejectAsync(long id, TechnicianAssignmentReasonDto request, CancellationToken cancellationToken = default);
        Task<TechnicianAssignmentDetailDto> StartAsync(long id, TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken = default);
        Task<TechnicianAssignmentDetailDto> CompleteAsync(long id, TechnicianAssignmentTransitionDto request, CancellationToken cancellationToken = default);
        Task<TechnicianAssignmentDetailDto> CancelAsync(long id, TechnicianAssignmentReasonDto request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TechnicianAssignmentActionItemDto>> GetActionsAsync(long id, CancellationToken cancellationToken = default);
        Task<TechnicianAssignmentActionItemDto> AddActionAsync(long id, TechnicianAssignmentActionCreateDto request, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TechnicianAssignmentStatusItemDto>> GetStatusesAsync(CancellationToken cancellationToken = default);
    }
}
