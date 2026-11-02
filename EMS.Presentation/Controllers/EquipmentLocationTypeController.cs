using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentLocationType;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentLocationTypeListItemDto>>> GetAllList([FromQuery] EquipmentLocationTypeListRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _service.GetPagedAsync(request, cancellationToken); 
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                return NotFound(new { message = "نوع محل تجهیز موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> Create([FromBody] EquipmentLocationTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentLocationTypeId }, result);
        }

        [Authorize]
        //[HttpPut("{id:long}")]
        [HttpPut]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> Update([FromBody] EquipmentLocationTypeUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(request, cancellationToken);
            if (result is null)
                return NotFound(new { message = "نوع محل تجهیز موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "نوع محل تجهیز موردنظر پیدا نشد." });
            return NoContent();
        }
    }
}
