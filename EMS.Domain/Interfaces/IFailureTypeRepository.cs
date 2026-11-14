using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces
{
    public interface IFailureTypeRepository : ICatalogRepository<FailureType>
    {
        /// <summary>The ids of the equipment types a failure type is linked to (empty = it applies to every type).</summary>
        Task<List<long>> GetEquipmentTypeIdsAsync(long failureTypeId, CancellationToken cancellationToken = default);

        /// <summary>The ACTIVE equipment types of each of these failure types, with their titles (one query).</summary>
        Task<Dictionary<long, List<(long Id, string Title)>>> GetEquipmentTypesAsync(IReadOnlyCollection<long> failureTypeIds, CancellationToken cancellationToken = default);

        /// <summary>Makes the links of an EXISTING failure type exactly <paramref name="equipmentTypeIds"/> (the change is saved with the unit of work).</summary>
        Task ReplaceEquipmentTypesAsync(long failureTypeId, IReadOnlyCollection<long> equipmentTypeIds, CancellationToken cancellationToken = default);
    }
}
