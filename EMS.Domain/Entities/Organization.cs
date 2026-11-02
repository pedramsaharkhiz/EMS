using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class Organization : BaseEntity, IAuditableEntity
    {
        public long OrganizationId { get; set; }
        public long? FieldId { get; set; }
        public long? OrganizationManagementId { get; set; }
        public long OrganizationTypeId { get; set; }
        public string Title { get; set; } = null!;
        public string? Code { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public Field? Field { get; set; }
        public OrganizationManagement? OrganizationManagement { get; set; }
        public OrganizationType OrganizationType { get; set; } = null!;
        public ICollection<Equipment> CurrentEquipments { get; set; } = new List<Equipment>();
        public ICollection<EquipmentLocationHistory> EquipmentLocationHistories { get; set; } = new List<EquipmentLocationHistory>();
        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}