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

        /// <summary>
        /// Like <see cref="UploadAsync"/> but inside a transaction the caller already opened: it neither opens nor commits one and does not
        /// audit. The caller commits, then calls <see cref="AuditUploaded"/> for each item. Used to register something together with its files.
        /// </summary>
        Task<AttachmentItemDto> UploadInTransactionAsync(AttachmentTarget target, long entityId, AttachmentUpload upload, CancellationToken cancellationToken = default);

        /// <summary>Writes the audit entry of an upload made with <see cref="UploadInTransactionAsync"/> (after the commit).</summary>
        void AuditUploaded(AttachmentItemDto item);

        /// <summary>
        /// The active attachments of a record, newest first, without checking that the record exists (the caller has just loaded it, so a
        /// deactivated record still lists its files). Empty when attachments are not enabled for the table.
        /// </summary>
        Task<IReadOnlyList<AttachmentItemDto>> ListActiveAsync(string tableName, long entityId, CancellationToken cancellationToken = default);

        /// <summary>How many active attachments each of these records has (records with none are absent). One query for all of them.</summary>
        Task<Dictionary<long, int>> CountActiveAsync(string tableName, IReadOnlyCollection<long> entityIds, CancellationToken cancellationToken = default);

        /// <summary>The file of an active attachment. The caller disposes it after streaming it to the client.</summary>
        Task<(AttachmentItemDto Item, AttachmentDownload File)> DownloadAsync(long attachmentId, CancellationToken cancellationToken = default);

        /// <summary>Deactivates the attachment (the row stays; it disappears from the default list and cannot be downloaded).</summary>
        Task DeleteAsync(long attachmentId, CancellationToken cancellationToken = default);
    }
}
