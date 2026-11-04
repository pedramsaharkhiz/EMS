using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentLocationType;
using EMS.Application.Interfaces.EquipmentLocationType;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentLocationTypeController : BaseApiController
    {
        private readonly IEquipmentLocationTypeService _service;

        public EquipmentLocationTypeController(IEquipmentLocationTypeService service)
        {
            _service = service;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentLocationTypeListItemDto>>>> GetAllList([FromQuery] EquipmentLocationTypeListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentLocationTypeDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentLocationTypes.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<EquipmentLocationTypeDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentLocationTypeDetailDto>>> Create([FromBody] EquipmentLocationTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.EquipmentLocationTypes.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentLocationTypeDetailDto>>> Update(long id, [FromBody] EquipmentLocationTypeUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.EquipmentLocationTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
