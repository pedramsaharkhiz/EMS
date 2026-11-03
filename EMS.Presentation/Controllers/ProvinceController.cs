using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Province;
using EMS.Application.Interfaces.Province;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class ProvinceController : BaseApiController
    {
        private readonly IProvinceService _service;

        public ProvinceController(IProvinceService service)
        {
            _service = service;
        }

        [Permission(Permissions.Provinces.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ProvinceListItemDto>>>> GetAllList([FromQuery] ProvinceListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.Provinces.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<ProvinceDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Provinces.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<ProvinceDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<ProvinceDetailDto>>> Create([FromBody] ProvinceCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.Provinces.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<ProvinceDetailDto>>> Update(long id, [FromBody] ProvinceUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.Provinces.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
