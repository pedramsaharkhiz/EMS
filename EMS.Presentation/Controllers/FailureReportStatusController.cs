using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.FailureReportStatus;
using EMS.Application.Interfaces.FailureReportStatus;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class FailureReportStatusController : BaseApiController
    {
        private readonly IFailureReportStatusService _service;

        public FailureReportStatusController(IFailureReportStatusService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<FailureReportStatusListItemDto>>>> GetAllList([FromQuery] FailureReportStatusListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureReportStatusDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.FailureReportStatuses.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<FailureReportStatusDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<FailureReportStatusDetailDto>>> Create([FromBody] FailureReportStatusCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.FailureReportStatuses.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureReportStatusDetailDto>>> Update(long id, [FromBody] FailureReportStatusUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.FailureReportStatuses.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
