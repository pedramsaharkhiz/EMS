using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Interfaces.EquipmentStatus;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentStatusController : BaseApiController
    {
        private readonly IEquipmentStatusService _service;

        public EquipmentStatusController(IEquipmentStatusService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentStatuses.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentStatusListItemDto>>> GetAllList([FromQuery] EquipmentStatusListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentStatuses.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentStatusDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentStatuses.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentStatusDetailDto>> Create([FromBody] EquipmentStatusCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentStatusId }, result);
        }

        [Permission(Permissions.EquipmentStatuses.Update)]
        [HttpPut]
        public async Task<ActionResult<EquipmentStatusDetailDto>> Update([FromBody] EquipmentStatusUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentStatuses.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
