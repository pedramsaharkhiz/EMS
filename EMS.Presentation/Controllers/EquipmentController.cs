using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Dtos.EquipmentLocationHistory;
using EMS.Application.Interfaces.Equipment;
using EMS.Domain.Enums;
using EMS.Presentation.Conventions;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class EquipmentController : BaseApiController
    {
        private readonly IEquipmentService _service;

        public EquipmentController(IEquipmentService service)
        {
            _service = service;
        }

        [Permission(Permissions.Equipments.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentListItemDto>>>> GetAllList([FromQuery] EquipmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        /// <summary>The Excel file of the equipment that match the same filters as the list (paging is ignored).</summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("export")]
        [ExcelResponse]
        public async Task<IActionResult> Export([FromQuery] EquipmentListRequestDto request, CancellationToken cancellationToken) =>
            ExcelDownload(await _service.ExportAsync(request, cancellationToken));

        /// <summary>Users that can be chosen as the technician of an equipment (dropdown of the equipment form).</summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("technicians")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianItemDto>>>> GetTechnicians([FromQuery] TechnicianListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetTechniciansAsync(request, cancellationToken));

        [Permission(Permissions.Equipments.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.Equipments.View)]
        [HttpGet("{id:long}/location-history")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentLocationHistoryItemDto>>>> GetLocationHistory(long id, [FromQuery] EquipmentLocationHistoryRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetLocationHistoryAsync(id, request, cancellationToken));

        [Permission(Permissions.Equipments.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<EquipmentDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentDetailDto>>> Create([FromBody] EquipmentCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>Creates several equipment at once (each item is a complete create request). All or nothing; max 200 items.</summary>
        [Permission(Permissions.Equipments.Create)]
        [HttpPost("bulk")]
        [ProducesResponseType<ApiResponse<IReadOnlyList<EquipmentDetailDto>>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentDetailDto>>>> CreateBulk([FromBody] EquipmentBulkCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateBulkAsync(request, cancellationToken);
            return CreatedAt(nameof(GetAllList), new { }, result);
        }

        [Permission(Permissions.Equipments.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentDetailDto>>> Update(long id, [FromBody] EquipmentUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        /// <summary>Moves the equipment to a new location and records it in the location history.</summary>
        [Permission(Permissions.Equipments.Assign)]
        [HttpPost("{id:long}/move")]
        public async Task<ActionResult<ApiResponse<EquipmentDetailDto>>> Move(long id, [FromBody] EquipmentMoveDto request, CancellationToken cancellationToken) =>
            Updated(await _service.MoveAsync(id, request, cancellationToken), "تجهیز با موفقیت جابه‌جا شد.");

        [Permission(Permissions.Equipments.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }

        /// <summary>
        /// فهرست مالکان قابل انتخاب برای تجهیز.
        /// </summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("owners")]
        public ActionResult<ApiResponse<IReadOnlyList<EquipmentOwnerItemDto>>> GetOwners()
        {
            IReadOnlyList<EquipmentOwnerItemDto> owners =
                Enum.GetValues<EquipmentOwner>()
                    .Select(owner => new EquipmentOwnerItemDto
                    {
                        Id = (int)owner,
                        Name = owner.ToString(),
                        Title = owner switch
                        {
                            EquipmentOwner.Company => "شرکت",
                            EquipmentOwner.Bank => "بانک",
                            _ => owner.ToString()
                        }
                    })
                    .ToList();

            return Success(owners);
        }

    }
}
