using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.FailureType;
using EMS.Application.Interfaces.FailureType;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class FailureTypeController : BaseApiController
    {
        private readonly IFailureTypeService _service;

        public FailureTypeController(IFailureTypeService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<FailureTypeListItemDto>>>> GetAllList([FromQuery] FailureTypeListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureTypeDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.FailureTypes.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<FailureTypeDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<FailureTypeDetailDto>>> Create([FromBody] FailureTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.FailureTypes.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureTypeDetailDto>>> Update(long id, [FromBody] FailureTypeUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.FailureTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
