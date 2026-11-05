using EMS.Application.Common.Exceptions;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Interfaces.Attachment;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace EMS.Presentation.Conventions
{
    /// <summary>
    /// Reads the body of POST /api/Equipment/with-attachments, a multipart/form-data request that is streamed, never buffered:
    /// first a text part named <c>equipment</c> with the same JSON as a plain registration, then any number of file parts. A text part named
    /// <c>description</c> right before a file is that file's description. Anything else is ignored.
    /// </summary>
    internal static class EquipmentRegistrationRequest
    {
        public const string EquipmentPart = "equipment";
        public const string DescriptionPart = "description";

        private const int MaxJsonBytes = 64 * 1024;
        private const int MaxDescriptionBytes = 4 * 1024;

        public static MultipartReader CreateReader(HttpRequest request)
        {
            if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaType) ||
                !mediaType.MediaType.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                throw new UnsupportedBodyException();
            }

            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary) || boundary.Length > 128)
                throw new BadRequestException("درخواست نامعتبر است: مرز (boundary) بدنه‌ی multipart درست نیست.", ErrorCodes.BadRequest);

            return new MultipartReader(boundary, request.Body);
        }

        /// <summary>The first part: the equipment JSON, parsed and validated with the same rules as a plain registration.</summary>
        public static async Task<EquipmentCreateDto> ReadEquipmentAsync(
            MultipartReader reader, JsonSerializerOptions json, IValidator<EquipmentCreateDto> validator, CancellationToken cancellationToken)
        {
            var section = await reader.ReadNextSectionAsync(cancellationToken);
            var disposition = section is null ? null : ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var parsed) ? parsed : null;

            if (section is null || disposition is null || disposition.IsFileDisposition()
                || !string.Equals(HeaderUtilities.RemoveQuotes(disposition.Name).Value, EquipmentPart, StringComparison.Ordinal))
            {
                throw Invalid("بخش اول درخواست باید متن JSON تجهیز با نام «equipment» باشد (پیش از هر فایل).");
            }

            var text = await ReadSmallTextAsync(section.Body, MaxJsonBytes, cancellationToken)
                ?? throw Invalid("بخش «equipment» بیش از حد بزرگ است.");

            EquipmentCreateDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<EquipmentCreateDto>(text, json);
            }
            catch (JsonException)
            {
                throw Invalid("بخش «equipment» JSON معتبری نیست.");
            }

            if (dto is null)
                throw Invalid("بخش «equipment» خالی است.");

            await validator.ValidateAndThrowAsync(dto, cancellationToken);
            return dto;
        }

        /// <summary>The remaining parts as files, lazily: a file's stream must be read to the end before the next one is asked for.</summary>
        public static async IAsyncEnumerable<AttachmentUpload> ReadFilesAsync(MultipartReader reader, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            string? description = null;

            while (await reader.ReadNextSectionAsync(cancellationToken) is { } section)
            {
                var file = section.AsFileSection();
                if (file?.FileStream is not null)
                {
                    yield return new AttachmentUpload(file.FileName ?? string.Empty, file.FileStream, description);
                    description = null;
                    continue;
                }

                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var disposition)
                    && string.Equals(HeaderUtilities.RemoveQuotes(disposition.Name).Value, DescriptionPart, StringComparison.Ordinal))
                {
                    description = await ReadSmallTextAsync(section.Body, MaxDescriptionBytes, cancellationToken)
                        ?? throw new ValidationException(new[] { new ValidationFailure(DescriptionPart, "توضیحات پیوست بیش از حد طولانی است.") });
                }
            }
        }

        private static ValidationException Invalid(string message) =>
            new(new[] { new ValidationFailure(EquipmentPart, message) });

        /// <summary>The text of a small part, or null when it is longer than <paramref name="maxBytes"/> (so a huge part is never read into memory).</summary>
        private static async Task<string?> ReadSmallTextAsync(Stream body, int maxBytes, CancellationToken cancellationToken)
        {
            var buffer = new byte[maxBytes + 1];
            var total = 0;
            int read;
            while (total < buffer.Length && (read = await body.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken)) > 0)
                total += read;

            return total > maxBytes ? null : Encoding.UTF8.GetString(buffer, 0, total);
        }
    }

    /// <summary>The body is not multipart/form-data (a 415).</summary>
    internal sealed class UnsupportedBodyException : Exception
    {
    }
}
