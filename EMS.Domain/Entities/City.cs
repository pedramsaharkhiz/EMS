using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class City : BaseEntity, IAuditableEntity, ICatalogEntity
    {
        public long CityId { get; set; }
        public long ProvinceId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }

        public Province Province { get; set; } = null!;
        public ICollection<SupportingDepartment> SupportingDepartments { get; set; } = new List<SupportingDepartment>();
    }
}