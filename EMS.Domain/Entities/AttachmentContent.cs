using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    /// <summary>
    /// The bytes of an attached file live in the database (shadow column <c>Content</c>, varbinary(max)), never on the app server's
    /// disk. The column is deliberately not a CLR property: nothing can load a file into memory by accident. It is written and read
    /// as a stream by IAttachmentBlobStore; this entity only carries the metadata.
    /// </summary>
    public class AttachmentContent : BaseEntity, IAuditableEntity
    {
        public Guid StreamId { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long SizeBytes { get; set; }
        public byte[]? Sha256 { get; set; }
    }
}
