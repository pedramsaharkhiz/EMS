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
        /// <summary>The primary key of the record to update (implemented explicitly, so it is not part of the JSON contract).</summary>
        long EntityId { get; }

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
