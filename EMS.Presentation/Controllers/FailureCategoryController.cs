using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.FailureCategory;
using EMS.Application.Interfaces.FailureCategory;
using EMS.Presentation.Security;
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

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<FailureCategoryListItemDto>>>> GetAllList([FromQuery] FailureCategoryListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureCategoryDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.FailureCategories.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<FailureCategoryDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<FailureCategoryDetailDto>>> Create([FromBody] FailureCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.FailureCategories.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureCategoryDetailDto>>> Update(long id, [FromBody] FailureCategoryUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.FailureCategories.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
