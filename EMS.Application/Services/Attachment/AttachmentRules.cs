using EMS.Application.Common.Exceptions;

namespace EMS.Application.Services.Attachment
{
    /// <summary>
    /// What may be attached: the size limits, the allowed file types and the checks that keep a renamed file out. A type is accepted only
    /// when the extension is allowed AND the first bytes of the file really look like that type (the client's MIME type is never trusted;
    /// the content type that is stored and served comes from this table).
    /// </summary>
    public static class AttachmentRules
    {
        /// <summary>The biggest single file (10 MB).</summary>
        public const long MaxFileSizeBytes = 10L * 1024 * 1024;

        /// <summary>The request limit of the upload endpoint: the file plus the multipart framing.</summary>
        public const long MaxRequestBytes = MaxFileSizeBytes + 1024 * 1024;

        /// <summary>The most active attachments one record can have.</summary>
        public const int MaxActivePerEntity = 20;

        /// <summary>Files that may come with one equipment registration (POST /api/Equipment/with-attachments).</summary>
        public const int MaxFilesPerRegistration = 5;

        /// <summary>Total size of the files of one registration (each file is still limited by <see cref="MaxFileSizeBytes"/>).</summary>
        public const long MaxRegistrationBytes = 50L * 1024 * 1024;

        /// <summary>The request limit of that endpoint: the files plus the JSON part and the multipart framing.</summary>
        public const long MaxRegistrationRequestBytes = MaxRegistrationBytes + 1024 * 1024;

        public const int MaxDescriptionLength = 500;
        public const int MaxFileNameLength = 200;

        /// <summary>How many leading bytes are inspected to recognize the type.</summary>
        public const int HeadLength = 512;

        public sealed record FileType(string Extension, string ContentType, byte[]? Signature);

        private static readonly byte[] Zip = { 0x50, 0x4B, 0x03, 0x04 };

        public static readonly IReadOnlyList<FileType> AllowedTypes = new[]
        {
            new FileType(".pdf", "application/pdf", "%PDF-"u8.ToArray()),
            new FileType(".jpg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            new FileType(".jpeg", "image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
            new FileType(".png", "image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            new FileType(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Zip),
            new FileType(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Zip),
            new FileType(".txt", "text/plain", null),
        };

        public static string AllowedExtensionsText => string.Join("، ", AllowedTypes.Select(t => t.Extension.TrimStart('.')));

        /// <summary>A file name that is safe to store and to put in a header: no path, no control or reserved characters, bounded length.</summary>
        public static string SanitizeFileName(string? fileName)
        {
            var name = (fileName ?? string.Empty).Replace('\\', '/');
            name = name[(name.LastIndexOf('/') + 1)..];

            var invalid = new HashSet<char>("\"<>|:*?") ;
            name = new string(name.Where(c => !char.IsControl(c) && !invalid.Contains(c)).ToArray()).Trim().Trim('.');

            if (name.Length > MaxFileNameLength)
            {
                var extension = Path.GetExtension(name);
                name = name[..Math.Max(1, MaxFileNameLength - extension.Length)] + extension;
            }

            return name;
        }

        /// <summary>The type of an already sanitized file name, or null when its extension is not allowed.</summary>
        public static FileType? TypeOf(string sanitizedFileName)
        {
            var extension = Path.GetExtension(sanitizedFileName).ToLowerInvariant();
            return AllowedTypes.FirstOrDefault(t => t.Extension == extension);
        }

        /// <summary>Does the start of the file really look like <paramref name="type"/>?</summary>
        public static bool LooksLike(FileType type, ReadOnlySpan<byte> head)
        {
            if (type.Signature is not null)
                return head.StartsWith(type.Signature);

            // plain text: no NUL or other control characters (tab, line feed, form feed and carriage return are fine)
            foreach (var b in head)
            {
                if (b == 0x00 || (b < 0x20 && b is not (0x09 or 0x0A or 0x0C or 0x0D)))
                    return false;
            }

            return true;
        }
    }

    /// <summary>Reads at most a limit of bytes from the wrapped stream and refuses the upload as soon as the file is bigger (before it is stored).</summary>
    public sealed class LimitedStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _limit;
        private long _read;

        public LimitedStream(Stream inner, long limit)
        {
            _inner = inner;
            _limit = limit;
        }

        public long BytesRead => _read;

        private int Count(int n)
        {
            _read += n;
            if (_read > _limit)
                throw new BadRequestException($"حجم فایل نمی‌تواند بیشتر از {_limit / (1024 * 1024)} مگابایت باشد.", ErrorCodes.AttachmentTooLarge);
            return n;
        }

        public override int Read(byte[] buffer, int offset, int count) => Count(_inner.Read(buffer, offset, count));
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => Count(await _inner.ReadAsync(buffer, cancellationToken));
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>Gives back the bytes that were already read for inspection, then continues with the rest of the stream.</summary>
    public sealed class PrefixedStream : Stream
    {
        private readonly byte[] _prefix;
        private readonly Stream _rest;
        private int _position;

        public PrefixedStream(byte[] prefix, Stream rest)
        {
            _prefix = prefix;
            _rest = rest;
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_position < _prefix.Length)
            {
                var n = Math.Min(buffer.Length, _prefix.Length - _position);
                _prefix.AsSpan(_position, n).CopyTo(buffer);
                _position += n;
                return n;
            }

            return _rest.Read(buffer);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_position < _prefix.Length)
            {
                var n = Math.Min(buffer.Length, _prefix.Length - _position);
                _prefix.AsMemory(_position, n).CopyTo(buffer);
                _position += n;
                return n;
            }

            return await _rest.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
