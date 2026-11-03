using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class SupportingDepartmentController : BaseApiController
    {
        private readonly ISupportingDepartmentService _service;

        public SupportingDepartmentController(ISupportingDepartmentService service)
        {
            _service = service;
        }

        [Permission(Permissions.SupportingDepartments.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<SupportingDepartmentListItemDto>>>> GetAllList([FromQuery] SupportingDepartmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.SupportingDepartments.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.SupportingDepartments.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<SupportingDepartmentDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentDetailDto>>> Create([FromBody] SupportingDepartmentCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentDetailDto>>> Update(long id, [FromBody] SupportingDepartmentUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.SupportingDepartments.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
