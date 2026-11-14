using EMS.Domain.Interfaces;

namespace EMS.Domain.Entities;

public sealed class EmployeePhoneNumber : BaseEntity, IAuditableEntity
{
    public long EmployeePhoneNumberId { get; set; }
    public long EmployeeId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;
}
