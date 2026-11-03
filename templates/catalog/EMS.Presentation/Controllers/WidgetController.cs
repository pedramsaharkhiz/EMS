using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Widget;
using EMS.Application.Interfaces.Widget;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class WidgetController : BaseApiController
    {
        private readonly IWidgetService _service;

        public WidgetController(IWidgetService service)
        {
            _service = service;
        }

        [Permission(Permissions.__PLURAL__.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<WidgetListItemDto>>>> GetAllList([FromQuery] WidgetListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.__PLURAL__.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<WidgetDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.__PLURAL__.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<WidgetDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<WidgetDetailDto>>> Create([FromBody] WidgetCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.__PLURAL__.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<WidgetDetailDto>>> Update(long id, [FromBody] WidgetUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.__PLURAL__.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted();
        }
    }
}
