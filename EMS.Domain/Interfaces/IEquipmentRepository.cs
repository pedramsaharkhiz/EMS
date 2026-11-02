using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IEquipmentRepository : IRepository<Equipment>
    {
        Task<Equipment?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

        Task<List<Equipment>> GetByModelIdAsync(long equipmentModelId, CancellationToken cancellationToken = default);

        Task<List<Equipment>> GetByStatusIdAsync(long equipmentStatusId, CancellationToken cancellationToken = default);

        Task<List<Equipment>> GetActiveAsync(CancellationToken cancellationToken = default);

        Task<Equipment?> GetWithLatestLocationAsync(long id, CancellationToken cancellationToken = default);
    }
}
