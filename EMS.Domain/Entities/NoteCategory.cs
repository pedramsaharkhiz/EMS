using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class NoteCategory : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long NoteCategoryId { get; set; }
        public string Title { get; set; } = null!;
    }
}
