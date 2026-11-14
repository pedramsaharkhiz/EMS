using EMS.Application.Common.Models;
using EMS.Application.Dtos.EmployeePhoneNumber;

namespace EMS.Application.Interfaces.EmployeePhoneNumber;

public interface IEmployeePhoneNumberService
{
    Task<PagedResult<EmployeePhoneNumberItemDto>> GetPagedAsync(EmployeePhoneNumberListRequestDto request, CancellationToken ct);
    Task<EmployeePhoneNumberItemDto> GetByIdAsync(long id, CancellationToken ct);
    Task<EmployeePhoneNumberItemDto> CreateAsync(EmployeePhoneNumberCreateDto request, CancellationToken ct);
    Task<EmployeePhoneNumberItemDto> UpdateAsync(long id, EmployeePhoneNumberUpdateDto request, CancellationToken ct);
    Task DeleteAsync(long id, string? rowVersion, CancellationToken ct);
    Task BulkDeleteAsync(EmployeePhoneNumberBulkDeleteDto request, CancellationToken ct);
    // Stages changes only; the report service owns the transaction and saves report + phones together.
    Task ApplyForReportAsync(long? employeeId, List<EmployeePhoneNumberInputDto>? items, CancellationToken ct);
    Task<List<EmployeePhoneNumberItemDto>> GetForReportAsync(long? employeeId, CancellationToken ct);
}
