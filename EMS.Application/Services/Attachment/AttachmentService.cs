using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Logging;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Attachment;
using EMS.Application.Extensions;
using EMS.Application.Interfaces.Attachment;
using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Attachment
{
    /// <summary>
    /// Attachments of any attachable entity (the Tables registry): list, upload, download and deactivate. The file is streamed into the
    /// database in one transaction together with its rows, so an upload either completes or leaves nothing behind. Authorization (the
    /// permissions of the entity) is done by the caller with the <see cref="AttachmentTarget"/> this service hands out.
    /// </summary>
    public sealed class AttachmentService : IAttachmentService
    {
        private readonly IEntityAttachmentRepository _attachments;
        private readonly IRepository<AttachmentContent> _contents;
        private readonly IAttachmentTargetCatalog _targets;
        private readonly IAttachmentBlobStore _blobs;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditEventLogger _audit;

        public AttachmentService(
            IEntityAttachmentRepository attachments,
            IRepository<AttachmentContent> contents,
            IAttachmentTargetCatalog targets,
            IAttachmentBlobStore blobs,
            IUnitOfWork unitOfWork,
            IAuditEventLogger audit)
        {
            _attachments = attachments;
            _contents = contents;
            _targets = targets;
            _blobs = blobs;
            _unitOfWork = unitOfWork;
            _audit = audit;
        }

        private static NotFoundException TargetNotFound() =>
            new("پیوست برای این مورد فعال نیست یا موردنظر پیدا نشد.", ErrorCodes.AttachmentTargetNotFound);

        private static NotFoundException AttachmentNotFound() =>
            new("پیوست موردنظر پیدا نشد.", ErrorCodes.NotFound);

        public async Task<AttachmentTarget> GetTargetAsync(string tableName, CancellationToken cancellationToken = default) =>
            await _targets.FindAsync(tableName, cancellationToken) ?? throw TargetNotFound();

        public async Task<AttachmentTarget> GetTargetOfAttachmentAsync(long attachmentId, CancellationToken cancellationToken = default)
        {
            var tableId = await _attachments.QueryRows()
                .Where(x => x.Id == attachmentId && x.IsActive)
                .Select(x => (long?)x.TableId)
                .FirstOrDefaultAsync(cancellationToken);

            if (tableId is null)
                throw AttachmentNotFound();

            return await _targets.FindAsync(tableId.Value, cancellationToken) ?? throw AttachmentNotFound();
        }

        public async Task<PagedResult<AttachmentItemDto>> GetPagedAsync(AttachmentTarget target, long entityId, AttachmentListRequestDto request, CancellationToken cancellationToken = default)
        {
            if (!await _targets.EntityExistsAsync(target, entityId, cancellationToken))
                throw TargetNotFound();

            var active = request.IsActive ?? true;
            var tableName = target.TableName;

            return await _attachments.QueryRows()
                .Where(x => x.TableId == target.TableId && x.EntityId == entityId && x.IsActive == active)
                .OrderByDescending(x => x.Id)
                .Select(x => new AttachmentItemDto
                {
                    Id = x.Id,
                    TableName = tableName,
                    EntityId = x.EntityId,
                    FileName = x.FileName,
                    ContentType = x.ContentType,
                    SizeBytes = x.SizeBytes,
                    Description = x.Description,
                    IsActive = x.IsActive,
                    CreatedDateTime = x.CreatedDateTime,
                    CreatedByUserId = x.CreatedByUserId
                })
                .ToPagedResultAsync(request.PageNumber, request.PageSize, cancellationToken);
        }

        public async Task<AttachmentItemDto> UploadAsync(AttachmentTarget target, long entityId, AttachmentUpload upload, CancellationToken cancellationToken = default)
        {
            var item = await StoreAsync(target, entityId, upload, ownTransaction: true, cancellationToken);
            AuditUploaded(item);
            return item;
        }

        public Task<AttachmentItemDto> UploadInTransactionAsync(AttachmentTarget target, long entityId, AttachmentUpload upload, CancellationToken cancellationToken = default)
        {
            if (!_unitOfWork.HasActiveTransaction)
                throw new InvalidOperationException("UploadInTransactionAsync must run inside a transaction of the caller.");
            return StoreAsync(target, entityId, upload, ownTransaction: false, cancellationToken);
        }

        public void AuditUploaded(AttachmentItemDto item) => _audit.Created("EntityAttachment", item.Id);

        /// <summary>
        /// Validates and stores one file. <paramref name="ownTransaction"/>: this call commits by itself; otherwise it runs in the caller's
        /// transaction and the caller commits (and audits, with <see cref="AuditUploaded"/>, only after the commit).
        /// </summary>
        private async Task<AttachmentItemDto> StoreAsync(AttachmentTarget target, long entityId, AttachmentUpload upload, bool ownTransaction, CancellationToken cancellationToken)
        {
            if (!await _targets.EntityExistsAsync(target, entityId, cancellationToken))
                throw TargetNotFound();

            var description = string.IsNullOrWhiteSpace(upload.Description) ? null : upload.Description.Trim();
            if (description is { Length: > AttachmentRules.MaxDescriptionLength })
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure("description", $"توضیحات پیوست نمی‌تواند بیشتر از {AttachmentRules.MaxDescriptionLength} کاراکتر باشد.")
                });
            }

            var fileName = AttachmentRules.SanitizeFileName(upload.FileName);
            if (fileName.Length == 0)
                throw new BadRequestException("نام فایل معتبر نیست.", ErrorCodes.AttachmentFileRequired);

            var type = AttachmentRules.TypeOf(fileName)
                ?? throw new BadRequestException($"نوع این فایل مجاز نیست. نوع‌های مجاز: {AttachmentRules.AllowedExtensionsText}.", ErrorCodes.AttachmentTypeNotAllowed);

            if (await _attachments.CountActiveAsync(target.TableId, entityId, cancellationToken) >= AttachmentRules.MaxActivePerEntity)
                throw new ConflictException($"هر مورد حداکثر {AttachmentRules.MaxActivePerEntity} پیوست فعال می‌تواند داشته باشد.", ErrorCodes.AttachmentLimitReached);

            // Inspect the first bytes (the type must really be what the extension says), then stream the whole file on.
            var limited = new LimitedStream(upload.Content, AttachmentRules.MaxFileSizeBytes);
            var head = await ReadHeadAsync(limited, cancellationToken);
            if (head.Length == 0)
                throw new BadRequestException("فایل خالی است.", ErrorCodes.AttachmentFileRequired);
            if (!AttachmentRules.LooksLike(type, head))
                throw new BadRequestException("محتوای فایل با پسوند آن نمی‌خواند.", ErrorCodes.AttachmentContentMismatch);

            var content = new AttachmentContent { StreamId = Guid.NewGuid(), FileName = fileName, ContentType = type.ContentType };
            var attachment = new EntityAttachment
            {
                TableId = target.TableId,
                EntityId = entityId,
                StreamId = content.StreamId,
                Description = description
            };

            await using (var transaction = ownTransaction ? await _unitOfWork.BeginTransactionAsync(cancellationToken) : null)
            {
                await _contents.AddAsync(content, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var (size, sha256) = await _blobs.WriteAsync(content.StreamId, new PrefixedStream(head, limited), cancellationToken);
                content.SizeBytes = size;
                content.Sha256 = sha256;

                await _attachments.AddAsync(attachment, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
            }

            return new AttachmentItemDto
            {
                Id = attachment.EntityAttachmentId,
                TableName = target.TableName,
                EntityId = entityId,
                FileName = content.FileName,
                ContentType = content.ContentType,
                SizeBytes = content.SizeBytes,
                Description = attachment.Description,
                IsActive = attachment.IsActive,
                CreatedDateTime = attachment.CreatedDateTime,
                CreatedByUserId = attachment.CreatedByUserId
            };
        }

        public async Task<(AttachmentItemDto Item, AttachmentDownload File)> DownloadAsync(long attachmentId, CancellationToken cancellationToken = default)
        {
            var row = await _attachments.QueryRows().FirstOrDefaultAsync(x => x.Id == attachmentId && x.IsActive, cancellationToken)
                ?? throw AttachmentNotFound();
            var target = await _targets.FindAsync(row.TableId, cancellationToken) ?? throw AttachmentNotFound();
            var file = await _blobs.OpenReadAsync(row.StreamId, cancellationToken) ?? throw AttachmentNotFound();

            return (new AttachmentItemDto
            {
                Id = row.Id,
                TableName = target.TableName,
                EntityId = row.EntityId,
                FileName = row.FileName,
                ContentType = row.ContentType,
                SizeBytes = row.SizeBytes,
                Description = row.Description,
                IsActive = row.IsActive,
                CreatedDateTime = row.CreatedDateTime,
                CreatedByUserId = row.CreatedByUserId
            }, file);
        }

        public async Task DeleteAsync(long attachmentId, CancellationToken cancellationToken = default)
        {
            var attachment = await _attachments.GetAsync(attachmentId, cancellationToken) ?? throw AttachmentNotFound();
            if (!attachment.IsActive)
                return;

            attachment.IsActive = false;
            _attachments.Update(attachment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _audit.Deleted("EntityAttachment", attachment.EntityAttachmentId);
        }

        private static async Task<byte[]> ReadHeadAsync(Stream stream, CancellationToken cancellationToken)
        {
            var buffer = new byte[AttachmentRules.HeadLength];
            var read = 0;
            while (read < buffer.Length)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(read), cancellationToken);
                if (n == 0)
                    break;
                read += n;
            }

            return buffer[..read];
        }
    }
}
