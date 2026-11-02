using EMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMS.Domain.Interfaces
{
    public interface IEquipmentLocationHistoryRepository : IRepository<EquipmentLocationHistory>
    {
        Task<List<EquipmentLocationHistory>> GetHistoryByEquipmentIdAsync(long equipmentId, CancellationToken cancellationToken = default);

        Task<EquipmentLocationHistory?> GetLatestByEquipmentIdAsync(long equipmentId, CancellationToken cancellationToken = default);
    }
}
