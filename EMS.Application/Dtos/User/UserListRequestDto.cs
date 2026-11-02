using EMS.Application.Common.Models;

namespace EMS.Application.Dtos.User;

public sealed class UserListRequestDto : PagedRequest
{
    // جست‌وجوی عمومی در فیلدهای مختلف
    public string? Search { get; set; }

    // فیلترهای مستقل
    public long? Id { get; set; }

    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PersonnelCode { get; set; }

    public string? NationalCode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public long? SupportingDepartmentId { get; set; }

    public bool? IsActive { get; set; }
}