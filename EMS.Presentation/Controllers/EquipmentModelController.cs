using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Interfaces.EquipmentModel;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentModelController : BaseApiController
    {
        private readonly IEquipmentModelService _service;

        public EquipmentModelController(IEquipmentModelService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentModels.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentModelListItemDto>>> GetAllList([FromQuery] EquipmentModelListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentModels.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentModelDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentModels.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentModelDetailDto>> Create([FromBody] EquipmentModelCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentModelId }, result);
        }

        [Permission(Permissions.EquipmentModels.Update)]
        [HttpPut]
        public async Task<ActionResult<EquipmentModelDetailDto>> Update([FromBody] EquipmentModelUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentModels.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
