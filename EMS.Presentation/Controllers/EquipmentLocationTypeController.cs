using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Interfaces.EquipmentLocationType;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentLocationTypeController : BaseApiController
    {
        private readonly IEquipmentLocationTypeService _service;

        public EquipmentLocationTypeController(IEquipmentLocationTypeService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentLocationTypes.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentLocationTypeListItemDto>>> GetAllList([FromQuery] EquipmentLocationTypeListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentLocationTypes.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentLocationTypes.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> Create([FromBody] EquipmentLocationTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentLocationTypeId }, result);
        }

        [Permission(Permissions.EquipmentLocationTypes.Update)]
        [HttpPut]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> Update([FromBody] EquipmentLocationTypeUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentLocationTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
