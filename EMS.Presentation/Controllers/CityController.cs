using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.City;
using EMS.Application.Interfaces.City;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class CityController : BaseApiController
    {
        private readonly ICityService _service;

        public CityController(ICityService service)
        {
            _service = service;
        }

        [Permission(Permissions.Cities.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<CityListItemDto>>>> GetAllList([FromQuery] CityListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.Cities.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<CityDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Cities.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<CityDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<CityDetailDto>>> Create([FromBody] CityCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.Cities.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<CityDetailDto>>> Update(long id, [FromBody] CityUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.Cities.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
