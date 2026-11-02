namespace EMS.Domain.Interfaces
{
    /// <summary>
    /// A "catalog" (lookup / master-data) entity: has a unique, human-readable Title among its active records and is
    /// soft-deleted through <c>BaseEntity.IsActive</c>. Entities that implement this get their whole CRUD behaviour
    /// from the generic catalog implementation (see <c>CatalogService</c> / <c>CatalogRepository</c>).
    /// </summary>
    public interface ICatalogEntity
    {
        string Title { get; set; }
    }
}
