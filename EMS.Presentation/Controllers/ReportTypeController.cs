using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.ReportType;
using EMS.Application.Interfaces.ReportType;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class ReportTypeController : BaseApiController
    {
        private readonly IReportTypeService _service;

        public ReportTypeController(IReportTypeService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportTypeListItemDto>>>> GetAllList([FromQuery] ReportTypeListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<ReportTypeDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.ReportTypes.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<ReportTypeDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<ReportTypeDetailDto>>> Create([FromBody] ReportTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.ReportTypes.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<ReportTypeDetailDto>>> Update(long id, [FromBody] ReportTypeUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.ReportTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
