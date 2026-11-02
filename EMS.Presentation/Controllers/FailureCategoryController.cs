using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Interfaces.FailureCategory;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class FailureCategoryController : BaseApiController
    {
        private readonly IFailureCategoryService _service;

        public FailureCategoryController(IFailureCategoryService service)
        {
            _service = service;
        }

        [Permission(Permissions.FailureCategories.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<FailureCategoryListItemDto>>> GetAllList([FromQuery] FailureCategoryListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.FailureCategories.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<FailureCategoryDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.FailureCategories.Create)]
        [HttpPost]
        public async Task<ActionResult<FailureCategoryDetailDto>> Create([FromBody] FailureCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.FailureCategoryId }, result);
        }

        [Permission(Permissions.FailureCategories.Update)]
        [HttpPut]
        public async Task<ActionResult<FailureCategoryDetailDto>> Update([FromBody] FailureCategoryUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.FailureCategories.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
