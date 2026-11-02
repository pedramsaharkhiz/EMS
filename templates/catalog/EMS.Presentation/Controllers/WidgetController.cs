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
        public async Task<ActionResult<PagedResult<WidgetListItemDto>>> GetAllList([FromQuery] WidgetListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.__PLURAL__.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<WidgetDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.__PLURAL__.Create)]
        [HttpPost]
        public async Task<ActionResult<WidgetDetailDto>> Create([FromBody] WidgetCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.WidgetId }, result);
        }

        [Permission(Permissions.__PLURAL__.Update)]
        [HttpPut]
        public async Task<ActionResult<WidgetDetailDto>> Update([FromBody] WidgetUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.__PLURAL__.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
