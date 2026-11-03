using EMS.Application.Common.Models;

namespace EMS.Application.Interfaces
{
    /// <summary>
    /// The CRUD contract shared by all catalogs. A record that does not exist raises <c>NotFoundException</c>
    /// (rendered as 404 by the API), so callers never receive null.
    /// </summary>
    public interface ICatalogService<TCreate, TUpdate, TDetail, TListItem, TListRequest>
    {
        Task<PagedResult<TListItem>> GetPagedAsync(TListRequest request, CancellationToken cancellationToken = default);
        Task<TDetail> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<TDetail> CreateAsync(TCreate request, CancellationToken cancellationToken = default);
        Task<TDetail> UpdateAsync(long id, TUpdate request, CancellationToken cancellationToken = default);

        /// <summary>Soft delete: the record is deactivated, not removed.</summary>
        Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    }
}
