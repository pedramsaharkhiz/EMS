using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Attachment;
using EMS.Application.Interfaces.Attachment;
using EMS.Application.Services.Attachment;
using EMS.Presentation.Conventions;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace EMS.Presentation.Controllers
{
    /// <summary>
    /// Attachments of every entity that is marked [Attachable] (see the Tables registry), through one set of endpoints. The permissions
    /// are those of the entity the file is attached to: <c>{Group}.View</c> to list and download, <c>{Group}.Update</c> to upload and deactivate.
    /// Files are stored in the database as a stream, never on the app server's disk. See docs/ATTACHMENTS.md.
    /// </summary>
    public sealed class AttachmentsController : BaseApiController
    {
        private readonly IAttachmentService _service;
        private readonly IAuthorizationService _authorization;

        public AttachmentsController(IAttachmentService service, IAuthorizationService authorization)
        {
            _service = service;
            _authorization = authorization;
        }

        private async Task<bool> AllowedAsync(string permission) =>
            (await _authorization.AuthorizeAsync(User, null, $"Permission:{permission}")).Succeeded;

        /// <summary>The attachments of one record, newest first (<c>?isActive=false</c> lists the deactivated ones).</summary>
        [EntityPermission]
        [HttpGet("{tableName}/{entityId:long}")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<AttachmentItemDto>>>> GetAll(string tableName, long entityId, [FromQuery] AttachmentListRequestDto request, CancellationToken cancellationToken)
        {
            var target = await _service.GetTargetAsync(tableName, cancellationToken);
            if (!await AllowedAsync(target.ViewPermission))
                return Forbid();

            return Paged(await _service.GetPagedAsync(target, entityId, request, cancellationToken));
        }

        /// <summary>
        /// Uploads one file: a <c>multipart/form-data</c> body with the file as its (first) file part. The optional description goes in the
        /// query string (<c>?description=</c>) so the file part can be streamed without buffering. The body is read as a stream straight
        /// into the database.
        /// </summary>
        [EntityPermission]
        [HttpPost("{tableName}/{entityId:long}")]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(AttachmentRules.MaxRequestBytes)]
        [ProducesResponseType<ApiResponse<AttachmentItemDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<AttachmentItemDto>>> Upload(string tableName, long entityId, [FromQuery] string? description, CancellationToken cancellationToken)
        {
            var target = await _service.GetTargetAsync(tableName, cancellationToken);
            if (!await AllowedAsync(target.EditPermission))
                return Forbid();

            if (!MediaTypeHeaderValue.TryParse(Request.ContentType, out var mediaType) ||
                !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }

            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary) || boundary.Length > 128)
                throw new BadRequestException("درخواست آپلود نامعتبر است.", ErrorCodes.AttachmentFileRequired);

            var reader = new MultipartReader(boundary, Request.Body);
            while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
            {
                var file = section.AsFileSection();
                if (file?.FileStream is null)
                    continue;   // a plain form field: ignored

                var item = await _service.UploadAsync(target, entityId, new AttachmentUpload(file.FileName ?? string.Empty, file.FileStream, description), cancellationToken);
                return CreatedAt(nameof(Download), new { id = item.Id }, item);
            }

            throw new BadRequestException("فایلی ارسال نشده است.", ErrorCodes.AttachmentFileRequired);
        }

        /// <summary>Streams the file out of the database as a download (the content type is the one stored when the file was accepted).</summary>
        [EntityPermission]
        [HttpGet("{id:long}/download")]
        [FileDownloadResponse]
        public async Task<IActionResult> Download(long id, CancellationToken cancellationToken)
        {
            var target = await _service.GetTargetOfAttachmentAsync(id, cancellationToken);
            if (!await AllowedAsync(target.ViewPermission))
                return Forbid();

            var (item, file) = await _service.DownloadAsync(id, cancellationToken);
            Response.RegisterForDisposeAsync(file);
            Response.ContentLength = item.SizeBytes;
            return File(file.Content, item.ContentType, item.FileName);
        }

        /// <summary>Deactivates the attachment (soft delete).</summary>
        [EntityPermission]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            var target = await _service.GetTargetOfAttachmentAsync(id, cancellationToken);
            if (!await AllowedAsync(target.EditPermission))
                return Forbid();

            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("پیوست با موفقیت غیرفعال شد.");
        }
    }
}
