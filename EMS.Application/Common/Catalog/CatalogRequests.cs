using EMS.Application.Common.Models;

namespace EMS.Application.Common.Catalog
{
    /// <summary>What the generic catalog service needs to know about a create request.</summary>
    public interface ICatalogCreateRequest
    {
        string Title { get; }
    }

    /// <summary>What the generic catalog service needs to know about an update request.</summary>
    public interface ICatalogUpdateRequest
    {
        string? Title { get; }
        bool IsActive { get; }
        string? RowVersion { get; }
    }

    /// <summary>What the generic catalog service needs to know about a list request (paging comes from <see cref="PagedRequest"/>).</summary>
    public interface ICatalogListRequest
    {
        string? Title { get; }

        /// <summary>null = active records (default), true = active, false = deactivated.</summary>
        bool? IsActive { get; }
    }
}
