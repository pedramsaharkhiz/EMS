using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class OrganizationStructure : BaseEntity, IAuditableEntity
    {
        public long OrganizationStructureId { get; set; }
        public string? OrganizationStructureCode { get; set; }
        public string? Title { get; set; }
        public int? Grade { get; set; }
        public long? ParentOrganizationStructureId { get; set; }

        public OrganizationStructure? ParentOrganizationStructure { get; set; }
        public ICollection<OrganizationStructure> Children { get; set; } = new List<OrganizationStructure>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}