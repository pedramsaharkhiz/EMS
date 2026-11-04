using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class Post : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long PostId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
    }
}
