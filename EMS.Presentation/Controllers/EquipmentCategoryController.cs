using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.EquipmentCategory;
using EMS.Application.Interfaces.EquipmentCategory;
using EMS.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentCategoryController : BaseApiController
    {
        private readonly IEquipmentCategoryService _service;

        public EquipmentCategoryController(IEquipmentCategoryService service)
        {
            _service = service;
        }

        [Permission(Permissions.EquipmentCategories.View)]
        [HttpGet]
        public async Task<ActionResult<PagedResult<EquipmentCategoryListItemDto>>> GetAllList([FromQuery] EquipmentCategoryListRequestDto request, CancellationToken cancellationToken) =>
            Ok(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentCategories.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> GetById(long id, CancellationToken cancellationToken) =>
            Ok(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentCategories.Create)]
        [HttpPost]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> Create([FromBody] EquipmentCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.EquipmentCategoryId }, result);
        }

        [Permission(Permissions.EquipmentCategories.Update)]
        [HttpPut]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> Update([FromBody] EquipmentCategoryUpdateDto request, CancellationToken cancellationToken) =>
            Ok(await _service.UpdateAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentCategories.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
    }
}
