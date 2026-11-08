using EMS.Domain.Entities;
using EMS.Domain.ReadModels;

namespace EMS.Domain.Interfaces
{
    public interface IEquipmentRepository : IRepository<Equipment>
    {
        Task<Equipment?> GetDetailsAsync(long id, CancellationToken cancellationToken = default);

        Task<List<Equipment>> GetByModelIdAsync(long equipmentModelId, CancellationToken cancellationToken = default);

        Task<List<Equipment>> GetByStatusIdAsync(long equipmentStatusId, CancellationToken cancellationToken = default);

        Task<List<Equipment>> GetActiveAsync(CancellationToken cancellationToken = default);

        Task<Equipment?> GetWithLatestLocationAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>Loads an equipment for modification, active or not (a deactivated one can still be edited and restored).</summary>
        Task<Equipment?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>The equipment with these ids, tracked for update (active or not). An id that does not exist is simply absent from the result.</summary>
        Task<List<Equipment>> GetManyForUpdateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);

        /// <summary>Read-only flattened equipment: the active ones, or with false the deactivated ones.</summary>
        IQueryable<EquipmentRow> QueryRows(bool active);

        Task<EquipmentRow?> GetRowAsync(long id, CancellationToken cancellationToken = default);

        /// <summary>Does another equipment (active or not) already have this serial number? (any casing; the equipment <paramref name="exceptId"/> is not counted)</summary>
        Task<bool> SerialNumberExistsAsync(string serialNumber, long? exceptId = null, CancellationToken cancellationToken = default);

        /// <summary>Does another equipment (active or not) already have this asset number? (any casing; the equipment <paramref name="exceptId"/> is not counted)</summary>
        Task<bool> AssetNumberExistsAsync(string assetNumber, long? exceptId = null, CancellationToken cancellationToken = default);

        /// <summary>The location history of one equipment, newest first.</summary>
        IQueryable<EquipmentLocationHistoryRow> QueryLocationHistory(long equipmentId);

        void AddLocationHistory(EquipmentLocationHistory entry);

        /// <summary>Users that can be chosen as technician: active users that hold the post "Technician".</summary>
        IQueryable<TechnicianInfo> QueryTechnicians();

        /// <summary>
        /// Can these users be the technician of an equipment? The single place that decides it (the dropdown and every create / move use it).
        /// A user that does not exist is absent from the result.
        /// </summary>
        Task<Dictionary<long, TechnicianEligibility>> GetTechnicianEligibilityAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken = default);

        /// <summary>The current technician of an equipment, or null when it has none (or the user vanished). Active or not.</summary>
        Task<EquipmentTechnicianRow?> GetTechnicianOfEquipmentAsync(long equipmentId, CancellationToken cancellationToken = default);

        /// <summary>Which of these serial numbers (already trimmed) are held by an equipment, active or not? Returned in lower case.</summary>
        Task<HashSet<string>> FindExistingSerialNumbersAsync(IReadOnlyCollection<string> serialNumbers, CancellationToken cancellationToken = default);
        /// <summary>Which of these asset numbers (already trimmed) are held by an equipment, active or not? Returned in lower case.</summary>
        Task<HashSet<string>> FindExistingAssetNumbersAsync(IReadOnlyCollection<string> assetNumbers, CancellationToken cancellationToken = default);
        /// <summary>The flattened rows of these equipment, active or not.</summary>
        Task<List<EquipmentRow>> GetRowsAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    }
}
