using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Interfaces.EquipmentCategory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentCategoryController : BaseApiController
    {
        private readonly IEquipmentCategoryService _service;
        public EquipmentCategoryController(IEquipmentCategoryService service) 
        {
            _service = service;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentCategoryListItemDto>>> GetAllList([FromQuery] EquipmentCategoryListRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _service.GetPagedAsync(request, cancellationToken); 
            return Ok(result);
        }

        [Authorize]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                return NotFound(new { message = "دسته‌بندی موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> Create([FromBody] EquipmentCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentCategoryId }, result);
        }

        [Authorize]
        //[HttpPut("{id:long}")]
        [HttpPut]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> Update([FromBody] EquipmentCategoryUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(request, cancellationToken);
            if (result is null)
                return NotFound(new { message = "دسته‌بندی موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Authorize]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "دسته‌بندی موردنظر پیدا نشد." });
            return NoContent();
        }
    }
}
