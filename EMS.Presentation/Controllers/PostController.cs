using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Post;
using EMS.Application.Interfaces.Post;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class PostController : BaseApiController
    {
        private readonly IPostService _service;

        public PostController(IPostService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<PostListItemDto>>>> GetAllList([FromQuery] PostListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<PostDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Posts.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<PostDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<PostDetailDto>>> Create([FromBody] PostCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.Posts.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<PostDetailDto>>> Update(long id, [FromBody] PostUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.Posts.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
