namespace EMS.Domain.ReadModels;

/// <summary>Only the employee's branch scope is used for contact access; no active/branch-match validation is imposed.</summary>
public sealed class EmployeeContactRow
{
    public long EmployeePhoneNumberId { get; set; }
    public long EmployeeId { get; set; }
    public long? SupportingDepartmentId { get; set; }
}
