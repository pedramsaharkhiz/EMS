using EMS.Domain.Entities;

namespace EMS.Domain.Interfaces
{
    /// <summary>A branch as the scope endpoints see it: where it is and which department serves it now.</summary>
    public sealed record ScopeOrganizationRow(long Id, string Title, bool IsActive, long? SupportingDepartmentId, string? SupportingDepartmentTitle);

    public interface ISupportingDepartmentRepository : ICatalogRepository<SupportingDepartment>
    {
        /// <summary>The ids (of those given) that exist at the hierarchy level: "organization", "field", "organizationmanagement" or "zone".</summary>
        Task<HashSet<long>> GetExistingIdsAsync(string level, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);

        /// <summary>The ACTIVE branches under these fields, managements and zones (a branch hangs under a field or directly under a management).</summary>
        Task<List<long>> ResolveOrganizationIdsAsync(
            IReadOnlyCollection<long> fieldIds, IReadOnlyCollection<long> organizationManagementIds, IReadOnlyCollection<long> zoneIds,
            CancellationToken cancellationToken = default);

        /// <summary>Loads branches for modification (tracked), with the title of their current department.</summary>
        Task<List<Organization>> GetOrganizationsForUpdateAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);

        /// <summary>Titles of departments (active or not) by id.</summary>
        Task<Dictionary<long, string>> GetTitlesAsync(IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);

        /// <summary>How many ACTIVE branches this department serves.</summary>
        Task<int> CountOrganizationsAsync(long departmentId, CancellationToken cancellationToken = default);
    }
}
