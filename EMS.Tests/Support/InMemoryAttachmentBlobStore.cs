using EMS.Application.Interfaces.Attachment;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace EMS.Tests.Support
{
    /// <summary>
    /// The tests run on EF's in-memory database, which cannot execute the raw SQL of SqlAttachmentBlobStore. This keeps the bytes in a
    /// dictionary instead (the file is still read from the request as a stream). The SQL implementation is covered by the manual
    /// SqlAttachmentBlobStoreTests (needs a real SQL Server).
    /// </summary>
    public sealed class InMemoryAttachmentBlobStore : IAttachmentBlobStore
    {
        public static readonly ConcurrentDictionary<Guid, byte[]> Files = new();

        public async Task<(long SizeBytes, byte[] Sha256)> WriteAsync(Guid streamId, Stream content, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            Files[streamId] = bytes;
            return (bytes.LongLength, SHA256.HashData(bytes));
        }

        public Task<AttachmentDownload?> OpenReadAsync(Guid streamId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Files.TryGetValue(streamId, out var bytes) ? new AttachmentDownload(new MemoryStream(bytes, writable: false)) : null);
    }
}
