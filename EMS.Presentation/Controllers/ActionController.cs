using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.ActionEntity;
using EMS.Application.Interfaces.ActionEntity;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class ActionController : BaseApiController
    {
        private readonly IActionEntityService _service;

        public ActionController(IActionEntityService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ActionEntityListItemDto>>>> GetAllList([FromQuery] ActionEntityListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<ActionEntityDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Actions.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<ActionEntityDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<ActionEntityDetailDto>>> Create([FromBody] ActionEntityCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.Actions.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<ActionEntityDetailDto>>> Update(long id, [FromBody] ActionEntityUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.Actions.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
