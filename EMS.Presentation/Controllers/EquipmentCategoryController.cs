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
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentCategoryListItemDto>>>> GetAllList([FromQuery] EquipmentCategoryListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Permission(Permissions.EquipmentCategories.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentCategoryDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.EquipmentCategories.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<EquipmentCategoryDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentCategoryDetailDto>>> Create([FromBody] EquipmentCategoryCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.EquipmentCategories.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentCategoryDetailDto>>> Update(long id, [FromBody] EquipmentCategoryUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.EquipmentCategories.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }
    }
}
