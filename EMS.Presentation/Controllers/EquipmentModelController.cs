using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentModel;
using EMS.Application.Interfaces.EquipmentModel;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentModelController : BaseApiController
    {
        private readonly IEquipmentModelService _service;

        public EquipmentModelController(IEquipmentModelService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentModels.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentModelListItemDto>>>> GetAllList([FromQuery] EquipmentModelListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentModels.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentModelDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentModels.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<EquipmentModelDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentModelDetailDto>>> Create([FromBody] EquipmentModelCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.EquipmentModels.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentModelDetailDto>>> Update(long id, [FromBody] EquipmentModelUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.EquipmentModels.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted();
        }
    }
}
