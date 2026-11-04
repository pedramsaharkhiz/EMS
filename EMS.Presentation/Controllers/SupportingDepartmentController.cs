using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.SupportingDepartment;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class SupportingDepartmentController : BaseApiController
    {
        private readonly ISupportingDepartmentService _service;
        private readonly ISupportingDepartmentScopeService _scope;

        public SupportingDepartmentController(ISupportingDepartmentService service, ISupportingDepartmentScopeService scope)
        {
            _service = service;
            _scope = scope;
        }

        [Lookup]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IReadOnlyList<SupportingDepartmentListItemDto>>>> GetAllList([FromQuery] SupportingDepartmentListRequestDto request, CancellationToken cancellationToken) =>
            Paged(await _service.GetPagedAsync(request, cancellationToken));

        [Lookup]
        [HttpGet("{id:long}")]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentDetailDto>>> GetById(long id, CancellationToken cancellationToken) =>
            Success(await _service.GetByIdAsync(id, cancellationToken));

        [Permission(Permissions.SupportingDepartments.Create)]
        [HttpPost]
        [ProducesResponseType<ApiResponse<SupportingDepartmentDetailDto>>(StatusCodes.Status201Created)]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentDetailDto>>> Create([FromBody] SupportingDepartmentCreateDto request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAt(nameof(GetById), new { id = result.Id }, result);
        }

        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpPut("{id:long}")]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentDetailDto>>> Update(long id, [FromBody] SupportingDepartmentUpdateDto request, CancellationToken cancellationToken) =>
            Updated(await _service.UpdateAsync(id, request, cancellationToken));

        [Permission(Permissions.SupportingDepartments.Delete)]
        [HttpDelete("{id:long}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(long id, CancellationToken cancellationToken)
        {
            await _service.DeleteAsync(id, cancellationToken);
            return Deleted("با موفقیت غیرفعال شد.");
        }

        /// <summary>
        /// Puts branches under this department, one by one (organizationIds) and/or by group (fieldIds, organizationManagementIds, zoneIds:
        /// all their active branches). All or nothing. A branch has one department: one that already belongs to another refuses the
        /// request (409) unless moveFromOtherDepartments is true.
        /// </summary>
        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpPost("{id:long}/organizations")]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentOrganizationsResultDto>>> AssignOrganizations(long id, [FromBody] SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken) =>
            Success(await _scope.AssignAsync(id, request, cancellationToken), "شعبه‌ها به دپارتمان پشتیبانی اختصاص یافتند.");

        /// <summary>Takes the selected branches (same selection as assigning) off this department; branches of other departments are left alone.</summary>
        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpDelete("{id:long}/organizations")]
        public async Task<ActionResult<ApiResponse<SupportingDepartmentOrganizationsResultDto>>> RemoveOrganizations(long id, [FromBody] SupportingDepartmentOrganizationsDto request, CancellationToken cancellationToken) =>
            Success(await _scope.RemoveAsync(id, request, cancellationToken), "شعبه‌ها از دپارتمان پشتیبانی برداشته شدند.");
    }
}
