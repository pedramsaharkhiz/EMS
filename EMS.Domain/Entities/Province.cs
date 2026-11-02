using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class Province : BaseEntity, IAuditableEntity
    {
        public long ProvinceId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public ICollection<City> Cities { get; set; } = new List<City>();
    }
}