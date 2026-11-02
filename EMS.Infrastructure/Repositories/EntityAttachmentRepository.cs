using EMS.Domain.Entities;
using EMS.Domain.Interfaces;
using EMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Infrastructure.Repositories
{
    //public class EntityAttachmentRepository : Repository<EntityAttachment>, IEntityAttachmentRepository
    //{
    //    public EntityAttachmentRepository(EMSDbContext context) : base(context)
    //    {
    //    }

    //    public async Task<List<EntityAttachment>> GetByTableAndEntityAsync(long tableId, long entityId, CancellationToken cancellationToken = default)
    //    {
    //        return await _context.Set<EntityAttachment>()
    //            .AsNoTracking()
    //            .Include(x => x.FileTable)
    //            .Where(x => x.TableId == tableId && x.EntityId == entityId)
    //            .ToListAsync(cancellationToken);
    //    }

    //    public async Task<List<EntityAttachment>> GetByStreamIdAsync(Guid streamId, CancellationToken cancellationToken = default)
    //    {
    //        return await _context.Set<EntityAttachment>()
    //            .AsNoTracking()
    //            .Include(x => x.FileTable)
    //            .Where(x => x.StreamId == streamId)
    //            .ToListAsync(cancellationToken);
    //    }

    //    public async Task<bool> ExistsByTableAndEntityAsync(long tableId, long entityId, CancellationToken cancellationToken = default)
    //    {
    //        return await _context.Set<EntityAttachment>()
    //            .AnyAsync(x => x.TableId == tableId && x.EntityId == entityId, cancellationToken);
    //    }
    //}
}
