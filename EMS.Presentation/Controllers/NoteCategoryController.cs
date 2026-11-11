using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.NoteCategory;
using EMS.Application.Interfaces.NoteCategory;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class NoteCategoryController : BaseApiController
    {
        private readonly INoteCategoryService _service;

        public NoteCategoryController(INoteCategoryService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<NoteCategoryListItemDto>>>> GetAllList([FromQuery] NoteCategoryListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<NoteCategoryDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.NoteCategories.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<NoteCategoryDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<NoteCategoryDetailDto>>> Create([FromBody] NoteCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.NoteCategories.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<NoteCategoryDetailDto>>> Update(long id, [FromBody] NoteCategoryUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.NoteCategories.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
