using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.EmployeePhoneNumber;

public class EmployeePhoneNumberCreateDto
{
    public long EmployeeId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
}
public sealed class EmployeePhoneNumberUpdateDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? RowVersion { get; set; }
}
public sealed class EmployeePhoneNumberInputDto
{
    public long? Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? RowVersion { get; set; }
}
public sealed class EmployeePhoneNumberListRequestDto : PagedRequest
{
    public long? EmployeeId { get; set; }
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
}
public sealed class EmployeePhoneNumberDeleteItemDto
{
    public long Id { get; set; }
    public string? RowVersion { get; set; }
}
public sealed class EmployeePhoneNumberBulkDeleteDto
{
    public List<EmployeePhoneNumberDeleteItemDto> Items { get; set; } = [];
}
public sealed class EmployeePhoneNumberItemDto
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public long? ModifiedByUserId { get; set; }
    public DateTime? ModifiedDateTime { get; set; }
    public string RowVersion { get; set; } = "0";
}
