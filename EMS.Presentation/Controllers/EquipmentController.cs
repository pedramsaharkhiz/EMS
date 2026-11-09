using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Equipment;
using EMS.Application.Dtos.EquipmentLocationHistory;
using EMS.Application.Interfaces.Equipment;
using EMS.Domain;
using EMS.Domain.Enums;
using EMS.Presentation.Conventions;
using EMS.Presentation.Security;
using EMS.Application.Services.Attachment;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EMS.Presentation.Controllers
{
    public class EquipmentController : BaseApiController
    {
        private readonly IEquipmentService _service;
        private readonly IValidator<EquipmentCreateDto> _createValidator;
        private readonly JsonSerializerOptions _json;

        public EquipmentController(IEquipmentService service, IValidator<EquipmentCreateDto> createValidator, IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> json)
        {
            _service = service;
            _createValidator = createValidator;
            _json = json.Value.JsonSerializerOptions;
        }

        [Permission(Permissions.Equipments.View)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentListItemDto>>>> GetAllList([FromQuery] EquipmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        /// <summary>
        /// The equipment a user holds now (he is its technician), as the equipment list: every filter, sort and page parameter of the list works
        /// (<c>technicianUserId</c> is the route's user). Needs <c>Equipments.View</c>, like the list. 404 for an unknown user.
        /// </summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("~/api/Users/{userId:long}/equipment")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<EquipmentListItemDto>>>> GetOfUser(long userId, [FromQuery] EquipmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetOfUserAsync(userId, request, cancellationToken));

        /// <summary>The Excel file of the equipment that match the same filters as the list (paging is ignored).</summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("export")]
        [ExcelResponse]
        public async Task<IActionResult> Export([FromQuery] EquipmentListRequestDto request, CancellationToken cancellationToken) =>
            ExcelDownload(await _service.ExportAsync(request, cancellationToken));

        /// <summary>Users that can be chosen as the technician of an equipment (dropdown of the equipment form).</summary>
        [Lookup]
        [HttpGet("technicians")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<TechnicianItemDto>>>> GetTechnicians([FromQuery] TechnicianListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetTechniciansAsync(request, cancellationToken));

        /// <summary>
        /// The technician of this equipment with his post and supporting department; <c>data</c> is null when it has none. Works for a
        /// deactivated equipment too. (The dropdown of choosable technicians is GET /technicians.)
        /// </summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("{id:long}/technician")]
        public async Task<ActionResult<ApiResponse<EquipmentTechnicianDto?>>> GetTechnician(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetTechnicianAsync(id, cancellationToken));

        [Permission(Permissions.Equipments.View)]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<EquipmentDetailWithAttachmentsDto>>> GetById(long id, CancellationToken cancellationToken) =>
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

        /// <summary>
        /// Registers an equipment together with its files in ONE call, all or nothing (the plain <c>POST /api/Equipment</c> stays as it is).
        /// <c>multipart/form-data</c>, streamed: first a text part <c>equipment</c> with the same JSON as a plain registration, then up to 5
        /// files (10 MB each, 50 MB in total, the usual types). A text part <c>description</c> right before a file is its description.
        /// If anything is refused nothing is saved. Files are optional: with none it is a plain registration that answers with
        /// <c>attachments: []</c>. See docs/ATTACHMENTS.md.
        /// </summary>
        [Permission(Permissions.Equipments.Create)]
        [HttpPost("with-attachments")]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(AttachmentRules.MaxRegistrationRequestBytes)]
        [ProducesResponseType<ApiResponse<EquipmentRegistrationDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<EquipmentRegistrationDto>>> CreateWithAttachments(CancellationToken cancellationToken)
        {
            MultipartReader reader;
            try
            {
                reader = EquipmentRegistrationRequest.CreateReader(Request);
            }
            catch (UnsupportedBodyException)
            {
                return StatusCode(StatusCodes.Status415UnsupportedMediaType);
            }

            var equipment = await EquipmentRegistrationRequest.ReadEquipmentAsync(reader, _json, _createValidator, cancellationToken);
            var files = EquipmentRegistrationRequest.ReadFilesAsync(reader, cancellationToken);

            var result = await _service.CreateWithAttachmentsAsync(equipment, files, cancellationToken);
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

        /// <summary>
        /// Changes the equipment's place (a branch OR a supporting department) and/or its technician and records it in the location history. Every field
        /// is optional; what is not sent stays as it is; at least one change is required (see <c>EquipmentMoveDto</c>).
        /// </summary>
        [Permission(Permissions.Equipments.Assign)]
        [HttpPost("{id:long}/move")]
        public async Task<ActionResult<ApiResponse<EquipmentDetailDto>>> Move(long id, [FromBody] EquipmentMoveDto request, CancellationToken cancellationToken) =>
            Updated(await _service.MoveAsync(id, request, cancellationToken), "تجهیز با موفقیت جابه‌جا شد.");

        /// <summary>
        /// The same change (technician, branch or supporting department; see <c>move</c>) for several equipment at once, all or nothing, max 200.
        /// The answer counts what changed and what was already in that state.
        /// </summary>
        [Permission(Permissions.Equipments.Assign)]
        [HttpPost("bulk-move")]
        public async Task<ActionResult<ApiResponse<EquipmentBulkMoveResultDto>>> BulkMove([FromBody] EquipmentBulkMoveDto request, CancellationToken cancellationToken) =>
            Updated(await _service.BulkMoveAsync(request, cancellationToken), "جابه‌جایی گروهی با موفقیت انجام شد.");

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
        [Lookup]
        [HttpGet("owners")]
        public ActionResult<ApiResponse<IReadOnlyList<EquipmentOwnerItemDto>>> GetOwners()
        {
            IReadOnlyList<EquipmentOwnerItemDto> owners =
                Enum.GetValues<EquipmentOwner>()
                    .Select(owner => new EquipmentOwnerItemDto
                    {
                        Id = (int)owner,
                        Code = owner.ToString(),
                        Title = EquipmentOwnerTitles.TitleOf(owner)
                    })
                    .ToList();

            return Success(owners);
        }

        /// <summary>Returns active equipment at an organization, with search across Yaas code, serial number and asset number.</summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("by-organization")]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationEquipmentListItemDto>>>> GetOfOrganization([FromQuery] OrganizationEquipmentListRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _service.GetOfOrganizationAsync(request, cancellationToken);
            return Paged(result);
        }


    }
}
