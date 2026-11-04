using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Domain.ReadModels;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EMS.Infrastructure.Repositories
{
    public class EntityAttachmentRepository : Repository<EntityAttachment>, IEntityAttachmentRepository
    {
        public EntityAttachmentRepository(EMSDbContext context) : base(context) { }

        public IQueryable<AttachmentRow> QueryRows() =>
            from attachment in _context.EntityAttachments.AsNoTracking()
            join content in _context.AttachmentContents.AsNoTracking() on attachment.StreamId equals (Guid?)content.StreamId
            select new AttachmentRow
            {
                Id = attachment.EntityAttachmentId,
                TableId = attachment.TableId,
                EntityId = attachment.EntityId,
                StreamId = content.StreamId,
                FileName = content.FileName,
                ContentType = content.ContentType,
                SizeBytes = content.SizeBytes,
                Description = attachment.Description,
                IsActive = attachment.IsActive,
                CreatedDateTime = attachment.CreatedDateTime,
                CreatedByUserId = attachment.CreatedByUserId
            };

        public Task<int> CountActiveAsync(long tableId, long entityId, CancellationToken cancellationToken = default) =>
            _context.EntityAttachments.CountAsync(x => x.TableId == tableId && x.EntityId == entityId && x.IsActive, cancellationToken);

        public Task<EntityAttachment?> GetAsync(long attachmentId, CancellationToken cancellationToken = default) =>
            _context.EntityAttachments.FirstOrDefaultAsync(x => x.EntityAttachmentId == attachmentId, cancellationToken);
    }
}
