using EMS.Application.Common.Exceptions;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentLocationType;
using EMS.Application.Common.Security;
using EMS.Presentation.Extensions;
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

        [Permission(Permissions.EquipmentLocationTypes.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentLocationTypeListItemDto>>> GetAllList([FromQuery] EquipmentLocationTypeListRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _service.GetPagedAsync(request, cancellationToken); 
            return Ok(result);
        }

        [Permission(Permissions.EquipmentLocationTypes.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                throw new NotFoundException("نوع محل تجهیز موردنظر پیدا نشد.");
            return Ok(result);
        }

        [Permission(Permissions.EquipmentLocationTypes.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> Create([FromBody] EquipmentLocationTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentLocationTypeId }, result);
        }

        [Permission(Permissions.EquipmentLocationTypes.Update)]
        //[HttpPut("{id:long}")]
        [HttpPut]
        public async Task<ActionResult<EquipmentLocationTypeDetailDto>> Update([FromBody] EquipmentLocationTypeUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(request, cancellationToken);
            if (result is null)
                throw new NotFoundException("نوع محل تجهیز موردنظر پیدا نشد.");
            return Ok(result);
        }

        [Permission(Permissions.EquipmentLocationTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                throw new NotFoundException("نوع محل تجهیز موردنظر پیدا نشد.");
            return NoContent();
        }
    }
}
