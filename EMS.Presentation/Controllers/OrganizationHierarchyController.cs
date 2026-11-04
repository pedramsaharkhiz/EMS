using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.OrganizationHierarchy;
using EMS.Application.Interfaces.OrganizationHierarchy;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers;

public sealed class OrganizationHierarchyController : BaseApiController
{
    private readonly IOrganizationHierarchyService _service;

    public OrganizationHierarchyController(
        IOrganizationHierarchyService service)
    {
        _service = service;
    }

    [HttpGet]
    [Permission(Permissions.OrganizationHierarchy.View)]
    [ProducesResponseType<ApiResponse<OrganizationHierarchyResponseDto>>( StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<OrganizationHierarchyResponseDto>>> Get([FromQuery] OrganizationHierarchyRequestDto request,
            CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(
            request,
            cancellationToken);

        return Ok(new ApiResponse<OrganizationHierarchyResponseDto>
        {
            Success = true,
            Data = result.Data,
            Meta = new ApiMeta
            {
                Pagination = result.Pagination
            },
            TraceId = HttpContext.TraceIdentifier
        });
    }
}
