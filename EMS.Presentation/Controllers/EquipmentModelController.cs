using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentModel;
using EMS.Application.Common.Security;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<PagedResult<EquipmentModelListItemDto>>> GetAllList([FromQuery] EquipmentModelListRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _service.GetPagedAsync(request, cancellationToken); 
            return Ok(result);
        }

        [Permission(Permissions.EquipmentModels.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentModelDetailDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                return NotFound(new { message = "مدل تجهیز موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Permission(Permissions.EquipmentModels.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentModelDetailDto>> Create([FromBody] EquipmentModelCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentModelId }, result);
        }

        [Permission(Permissions.EquipmentModels.Update)]
        //[HttpPut("{id:long}")]
        [HttpPut]
        public async Task<ActionResult<EquipmentModelDetailDto>> Update([FromBody] EquipmentModelUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(request, cancellationToken);
            if (result is null)
                return NotFound(new { message = "مدل تجهیز موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Permission(Permissions.EquipmentModels.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "مدل تجهیز موردنظر پیدا نشد." });
            return NoContent();
        }
    }
}
