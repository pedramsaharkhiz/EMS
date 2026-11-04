using EMS.Application.Common.Models;
using EMS.Application.Dtos.Attachment;

namespace EMS.Application.Interfaces.Attachment
{
    public interface IAttachmentService
    {
        /// <summary>The target of a table name, or a 404 when the table is unknown or attachments are not enabled for it.</summary>
        Task<AttachmentTarget> GetTargetAsync(string tableName, CancellationToken cancellationToken = default);

        /// <summary>The target an existing attachment belongs to (404 when the attachment does not exist or was deactivated).</summary>
        Task<AttachmentTarget> GetTargetOfAttachmentAsync(long attachmentId, CancellationToken cancellationToken = default);

        Task<PagedResult<AttachmentItemDto>> GetPagedAsync(AttachmentTarget target, long entityId, AttachmentListRequestDto request, CancellationToken cancellationToken = default);

        Task<AttachmentItemDto> UploadAsync(AttachmentTarget target, long entityId, AttachmentUpload upload, CancellationToken cancellationToken = default);

        /// <summary>The file of an active attachment. The caller disposes it after streaming it to the client.</summary>
        Task<(AttachmentItemDto Item, AttachmentDownload File)> DownloadAsync(long attachmentId, CancellationToken cancellationToken = default);

        /// <summary>Deactivates the attachment (the row stays; it disappears from the default list and cannot be downloaded).</summary>
        Task DeleteAsync(long attachmentId, CancellationToken cancellationToken = default);
    }
}
