using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IEntityAttachmentRepository : IRepository<EntityAttachment>
    {
        /// <summary>Read-only query over all attachments (active or not) with their file metadata; the caller filters.</summary>
        IQueryable<EMS.Domain.ReadModels.AttachmentRow> QueryRows();

        Task<int> CountActiveAsync(long tableId, long entityId, CancellationToken cancellationToken = default);

        /// <summary>The attachment as tracked entity, active or not (null when it does not exist).</summary>
        Task<EntityAttachment?> GetAsync(long attachmentId, CancellationToken cancellationToken = default);
    }
}
