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
        public async Task<ActionResult<ApiResponse<IReadOnlyList<BrandListItemDto>>>> GetAllList([FromQuery] BrandListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.Brands.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<BrandDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Brands.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<BrandDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<BrandDetailDto>>> Create([FromBody] BrandCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.Brands.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<BrandDetailDto>>> Update(long id, [FromBody] BrandUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.Brands.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted();
        }
    }
}
