using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EmployeePhoneNumber;
using EMS.Application.Interfaces.EmployeePhoneNumber;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

public sealed class EmployeePhoneNumberController(IEmployeePhoneNumberService service) : BaseApiController
{
    [Permission(Permissions.EmployeePhoneNumbers.View)]
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<EmployeePhoneNumberItemDto>>>> GetPaged(
        [FromQuery] EmployeePhoneNumberListRequestDto request, CancellationToken cancellationToken) => Paged(await service.GetPagedAsync(request, cancellationToken));

    [Permission(Permissions.EmployeePhoneNumbers.View)]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResponse<EmployeePhoneNumberItemDto>>> GetById(long id, CancellationToken cancellationToken) =>
        Success(await service.GetByIdAsync(id, cancellationToken));

    [Permission(Permissions.EmployeePhoneNumbers.Create)]
    [HttpPost]
    [ProducesResponseType<ApiResponse<EmployeePhoneNumberItemDto>>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<EmployeePhoneNumberItemDto>>> Create([FromBody] EmployeePhoneNumberCreateDto request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAt(nameof(GetById), new { id = result.Id }, result);
    }
    [Permission(Permissions.EmployeePhoneNumbers.Update)]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<ApiResponse<EmployeePhoneNumberItemDto>>> Update(long id, [FromBody] EmployeePhoneNumberUpdateDto request, CancellationToken cancellationToken) =>
        Updated(await service.UpdateAsync(id, request, cancellationToken));

    [Permission(Permissions.EmployeePhoneNumbers.Delete)]
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(long id, [FromQuery] string? rowVersion, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, rowVersion, cancellationToken);
        return Deleted();
    }
    [Permission(Permissions.EmployeePhoneNumbers.Delete)]
    [HttpPost("bulk-delete")]
    public async Task<ActionResult<ApiResponse<object>>> BulkDelete([FromBody] EmployeePhoneNumberBulkDeleteDto request, CancellationToken cancellationToken)
    {
        await service.BulkDeleteAsync(request, cancellationToken);
        return Deleted();
    }
}
