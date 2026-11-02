using EMS.Application.Common.Models;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Application.Interfaces.Brand;
using EMS.Application.Common.Security;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EMS.Application.Dtos.Brand;

namespace EMS.Presentation.Controllers
{
    public class BrandController : BaseApiController
    {
        private readonly IBrandService _service;
        public BrandController(IBrandService service) 
        {
            _service = service;
        }

        [Permission(Permissions.Brands.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<BrandListItemDto>>> GetAllList([FromQuery] BrandListRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _service.GetPagedAsync(request, cancellationToken); 
            return Ok(result);
        }

        [Permission(Permissions.Brands.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<BrandDetailDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                return NotFound(new { message = "برند موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Permission(Permissions.Brands.Create)]
        [HttpPost]
        public async Task<ActionResult<BrandDetailDto>> Create([FromBody] BrandCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.BrandId }, result);
        }

        [Permission(Permissions.Brands.Update)]
        //[HttpPut("{id:long}")]
        [HttpPut]
        public async Task<ActionResult<BrandDetailDto>> Update([FromBody] BrandUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(request, cancellationToken);
            if (result is null)
                return NotFound(new { message = "برند موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Permission(Permissions.Brands.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "برند موردنظر پیدا نشد." });
            return NoContent();
        }
    }
}
