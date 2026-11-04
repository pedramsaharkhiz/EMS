namespace EMS.Domain.ReadModels
{
    /// <summary>One attachment with the metadata of its file (never the bytes).</summary>
    public sealed class AttachmentRow
    {
        public long Id { get; set; }
        public long TableId { get; set; }
        public long EntityId { get; set; }
        public Guid StreamId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public long? CreatedByUserId { get; set; }
    }
}
