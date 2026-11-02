using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Interfaces.EquipmentType;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentTypeController : BaseApiController
    {
        private readonly IEquipmentTypeService _service;

        public EquipmentTypeController(IEquipmentTypeService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentTypes.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentTypeListItemDto>>> GetAllList([FromQuery] EquipmentTypeListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentTypes.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentTypeDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentTypes.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentTypeDetailDto>> Create([FromBody] EquipmentTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentTypeId }, result);
        }

        [Permission(Permissions.EquipmentTypes.Update)]
        [HttpPut]
        public async Task<ActionResult<EquipmentTypeDetailDto>> Update([FromBody] EquipmentTypeUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
