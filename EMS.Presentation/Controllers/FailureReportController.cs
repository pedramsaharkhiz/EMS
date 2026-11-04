using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.FailureReport;
using EMS.Application.Interfaces.FailureReport;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class FailureReportController : BaseApiController
    {
        private readonly IFailureReportService _service;

        public FailureReportController(IFailureReportService service)
        {
            _service = service;
        }

        [Permission(Permissions.FailureReports.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<FailureReportListItemDto>>>> GetAllList([FromQuery] FailureReportListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.FailureReports.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureReportDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        /// <summary>Registers a failure report (status Open, attributed to the signed-in user).</summary>
        [Permission(Permissions.FailureReports.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<FailureReportDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<FailureReportDetailDto>>> Create([FromBody] FailureReportCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.FailureReports.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<FailureReportDetailDto>>> Update(long id, [FromBody] FailureReportUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.FailureReports.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
