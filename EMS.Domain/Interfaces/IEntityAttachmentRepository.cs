using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IEntityAttachmentRepository : IRepository<EntityAttachment>
    {
        Task<List<EntityAttachment>> GetByTableAndEntityAsync(long table , long entityId, CancellationToken cancellationToken = default);

        Task<List<EntityAttachment>> GetByStreamIdAsync(Guid streamId, CancellationToken cancellationToken = default);

        Task<bool> ExistsByTableAndEntityAsync(long tableId, long entityId, CancellationToken cancellationToken = default);
    }
}
