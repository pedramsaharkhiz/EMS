using EMS.Application.Common.Models;
using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Dtos.RefreshToken;
using EMS.Application.Interfaces.Auth;
using EMS.Application.Interfaces.FailureCategory;
using EMS.Application.Common.Security;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<ActionResult<PagedResult<FailureCategoryListItemDto>>> GetAllList([FromQuery] FailureCategoryListRequestDto request, CancellationToken cancellationToken) 
        { 
            var result = await _service.GetPagedAsync(request, cancellationToken); 
            return Ok(result);
        }

        [Permission(Permissions.FailureCategories.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<FailureCategoryDetailDto>> GetById(long id, CancellationToken cancellationToken)
        {
            var result = await _service.GetByIdAsync(id, cancellationToken);
            if (result is null)
                return NotFound(new { message = "دسته‌بندی موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Permission(Permissions.FailureCategories.Create)]
        [HttpPost]
        public async Task<ActionResult<FailureCategoryDetailDto>> Create([FromBody] FailureCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.FailureCategoryId }, result);
        }

        [Permission(Permissions.FailureCategories.Update)]
        //[HttpPut("{id:long}")]
        [HttpPut]
        public async Task<ActionResult<FailureCategoryDetailDto>> Update([FromBody] FailureCategoryUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(request, cancellationToken);
            if (result is null)
                return NotFound(new { message = "دسته‌بندی موردنظر پیدا نشد." });
            return Ok(result);
        }

        [Permission(Permissions.FailureCategories.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            var deleted = await _service.DeleteAsync(id, cancellationToken);
            if (!deleted)
                return NotFound(new { message = "دسته‌بندی موردنظر پیدا نشد." });
            return NoContent();
        }
    }
}
