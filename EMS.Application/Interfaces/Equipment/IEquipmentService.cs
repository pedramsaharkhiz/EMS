using EMS.Application.Common.Export;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Interfaces.Attachment;
using EMS.Application.Dtos.EquipmentLocationHistory;

namespace EMS.Application.Interfaces.Equipment
{
    public interface IEquipmentService
    {
        Task<PagedResult<EquipmentListItemDto>> GetPagedAsync(EquipmentListRequestDto request, CancellationToken cancellationToken = default);
        /// <summary>The equipment with its active attachments (metadata, newest first).</summary>
        Task<EquipmentDetailWithAttachmentsDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<EquipmentDetailDto> CreateAsync(EquipmentCreateDto request, CancellationToken cancellationToken = default);
        /// <summary>Creates all items or none; the result is in the order of the request.</summary>
        Task<IReadOnlyList<EquipmentDetailDto>> CreateBulkAsync(EquipmentBulkCreateDto request, CancellationToken cancellationToken = default);
        /// <summary>Registers an equipment together with its files (all or nothing, one transaction). The files are streamed lazily.</summary>
        Task<EquipmentRegistrationDto> CreateWithAttachmentsAsync(EquipmentCreateDto request, IAsyncEnumerable<AttachmentUpload> files, CancellationToken cancellationToken = default);

        Task<EquipmentDetailDto> UpdateAsync(long id, EquipmentUpdateDto request, CancellationToken cancellationToken = default);

        /// <summary>Soft delete: deactivates the equipment (idempotent). Restore with an update with <c>isActive = true</c>.</summary>
        Task DeleteAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>Changes the location and records it in the history (a move to the current location changes nothing).</summary>
        Task<EquipmentDetailDto> MoveAsync(long id, EquipmentMoveDto request, CancellationToken cancellationToken = default);
        Task<EquipmentBulkMoveResultDto> BulkMoveAsync(EquipmentBulkMoveDto request, CancellationToken cancellationToken = default);

        Task<PagedResult<EquipmentLocationHistoryItemDto>> GetLocationHistoryAsync(long id, EquipmentLocationHistoryRequestDto request, CancellationToken cancellationToken = default);
        /// <summary>The current technician of the equipment with his post and department, or null when it has none (404 when the equipment does not exist).</summary>
        Task<EquipmentTechnicianDto?> GetTechnicianAsync(long equipmentId, CancellationToken cancellationToken = default);
        Task<PagedResult<TechnicianItemDto>> GetTechniciansAsync(TechnicianListRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>The Excel file of the equipment that match the same filters as the list.</summary>
        Task<ExcelFile> ExportAsync(EquipmentListRequestDto request, CancellationToken cancellationToken = default);
    }
}
