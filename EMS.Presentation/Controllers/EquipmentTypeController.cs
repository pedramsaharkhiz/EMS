using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentType;
using EMS.Application.Interfaces.EquipmentType;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentTypeController : BaseApiController
    {
        private readonly IEquipmentTypeService _service;

        public EquipmentTypeController(IEquipmentTypeService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentTypes.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentTypeListItemDto>>>> GetAllList([FromQuery] EquipmentTypeListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentTypes.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentTypeDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentTypes.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<EquipmentTypeDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentTypeDetailDto>>> Create([FromBody] EquipmentTypeCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.EquipmentTypes.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentTypeDetailDto>>> Update(long id, [FromBody] EquipmentTypeUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.EquipmentTypes.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted();
        }
    }
}
