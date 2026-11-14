using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities
{
    public class Employee : BaseEntity, IAuditableEntity
    {
        public long EmployeeId { get; set; }
        public string PersonnelCode { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string NationalCode { get; set; } = null!;
        public DateTime BirthDate { get; set; }
        public long OrganizationId { get; set; }
        public long OrganizationStructureId { get; set; }
        public string? ProfilePictureUrl { get; set; }

        public Organization Organization { get; set; } = null!;
        public OrganizationStructure OrganizationStructure { get; set; } = null!;
        public ICollection<EmployeePhoneNumber> PhoneNumbers { get; set; } = new List<EmployeePhoneNumber>();
        public ICollection<FailureReport> FailureReports { get; set; } = new List<FailureReport>();
    }
}