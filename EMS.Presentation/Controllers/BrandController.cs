using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Brand;
using EMS.Application.Interfaces.Brand;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<ActionResult<PagedResult<BrandListItemDto>>> GetAllList([FromQuery] BrandListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.Brands.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<BrandDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Brands.Create)]
        [HttpPost]
        public async Task<ActionResult<BrandDetailDto>> Create([FromBody] BrandCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.BrandId }, result);
        }

        [Permission(Permissions.Brands.Update)]
        [HttpPut]
        public async Task<ActionResult<BrandDetailDto>> Update([FromBody] BrandUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.Brands.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
