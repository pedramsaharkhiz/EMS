namespace EMS.Application.Interfaces.Attachment
{
    /// <summary>An entity that can have attachments and the permissions that guard them.</summary>
    public sealed record AttachmentTarget(long TableId, string TableName, string DisplayName, string ViewPermission, string EditPermission);

    /// <summary>The file as read back from storage; disposing it releases the database reader behind <see cref="Content"/>.</summary>
    public sealed class AttachmentDownload : IAsyncDisposable
    {
        private readonly IAsyncDisposable? _owner;

        public AttachmentDownload(Stream content, IAsyncDisposable? owner = null)
        {
            Content = content;
            _owner = owner;
        }

        public Stream Content { get; }

        public async ValueTask DisposeAsync()
        {
            await Content.DisposeAsync();
            if (_owner is not null)
                await _owner.DisposeAsync();
        }
    }

    /// <summary>Which entities are attachable (Tables registry + the [Attachable] attribute) and whether a record exists.</summary>
    public interface IAttachmentTargetCatalog
    {
        /// <summary>The target of a table name (e.g. "Equipments"); null when the table is unknown or attachments are disabled for it.</summary>
        Task<AttachmentTarget?> FindAsync(string tableName, CancellationToken cancellationToken = default);

        Task<AttachmentTarget?> FindAsync(long tableId, CancellationToken cancellationToken = default);

        /// <summary>True when the (active) record exists. Attachments have no foreign key to their record, so this is the integrity check.</summary>
        Task<bool> EntityExistsAsync(AttachmentTarget target, long entityId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Where the bytes of a file are kept: the database, as a stream in and a stream out, never the app server's disk.
    /// <see cref="WriteAsync"/> joins the current database transaction.
    /// </summary>
    public interface IAttachmentBlobStore
    {
        /// <summary>Streams <paramref name="content"/> into the file of <paramref name="streamId"/> and returns its size and SHA-256.</summary>
        Task<(long SizeBytes, byte[] Sha256)> WriteAsync(Guid streamId, Stream content, CancellationToken cancellationToken = default);

        /// <summary>Opens the file for reading; null when there is none. Dispose the result when done.</summary>
        Task<AttachmentDownload?> OpenReadAsync(Guid streamId, CancellationToken cancellationToken = default);
    }

    public sealed record AttachmentUpload(string FileName, Stream Content, string? Description);
}
