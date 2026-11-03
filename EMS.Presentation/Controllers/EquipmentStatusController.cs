using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentStatus;
using EMS.Application.Interfaces.EquipmentStatus;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentStatusController : BaseApiController
    {
        private readonly IEquipmentStatusService _service;

        public EquipmentStatusController(IEquipmentStatusService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentStatusListItemDto>>>> GetAllList([FromQuery] EquipmentStatusListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentStatusDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentStatuses.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<EquipmentStatusDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentStatusDetailDto>>> Create([FromBody] EquipmentStatusCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.EquipmentStatuses.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentStatusDetailDto>>> Update(long id, [FromBody] EquipmentStatusUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.EquipmentStatuses.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
