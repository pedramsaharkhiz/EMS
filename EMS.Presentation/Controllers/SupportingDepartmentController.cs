using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Presentation.Extensions;
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
        public async Task<ActionResult<PagedResult<SupportingDepartmentListItemDto>>> GetAllList([FromQuery] SupportingDepartmentListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.SupportingDepartments.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<SupportingDepartmentDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.SupportingDepartments.Create)]
        [HttpPost]
        public async Task<ActionResult<SupportingDepartmentDetailDto>> Create([FromBody] SupportingDepartmentCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.SupportingDepartmentId }, result);
        }

        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpPut]
        public async Task<ActionResult<SupportingDepartmentDetailDto>> Update([FromBody] SupportingDepartmentUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.SupportingDepartments.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
