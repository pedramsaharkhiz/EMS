using EMS.Application.Common.Models;
using EMS.Application.Dtos.TechnicianAssignment;
using EMS.Application.Dtos.TechnicianAssignmentStatus;

namespace EMS.Application.Interfaces.TechnicianAssignment
{
    public interface ITechnicianAssignmentService
    {
        Task<PagedResult<TechnicianAssignmentListItemDto>> GetPagedAsync(TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default);
        Task<TechnicianAssignmentDetailDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>The assignments of one report (its history); 404 for an unknown report.</summary>
        Task<PagedResult<TechnicianAssignmentListItemDto>> GetOfReportAsync(long failureReportId, TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>The assignments of one user as technician; 404 for an unknown user.</summary>
        Task<PagedResult<TechnicianAssignmentListItemDto>> GetOfUserAsync(long userId, TechnicianAssignmentListRequestDto request, CancellationToken cancellationToken = default);

        Task<TechnicianAssignmentDetailDto> AssignAsync(long failureReportId, TechnicianAssignmentCreateDto request, CancellationToken cancellationToken = default);

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
