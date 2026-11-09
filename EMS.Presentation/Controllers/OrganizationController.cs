using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Organization;
using EMS.Application.Interfaces.SupportingDepartment;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    /// <summary>Manages organization-side relationships.</summary>
    public sealed class OrganizationController : BaseApiController
    {
        private readonly ISupportingDepartmentScopeService _scope;

        public OrganizationController(ISupportingDepartmentScopeService scope)
        {
            _scope = scope;
        }

        /// <summary>Returns the branch's current supporting department relationship.</summary>
        [Lookup]
        [HttpGet("{id:long}/supporting-department")]
        public async Task<ActionResult<ApiResponse<OrganizationSupportingDepartmentDto>>> GetSupportingDepartment(long id, CancellationToken cancellationToken)
        {
            var result = await _scope.GetOrganizationDepartmentAsync(id, cancellationToken);
            return Success(result);
        }

        /// <summary>Assigns or changes the branch's supporting department. Moving requires explicit confirmation.</summary>
        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpPut("{id:long}/supporting-department")]
        public async Task<ActionResult<ApiResponse<OrganizationSupportingDepartmentDto>>> SetSupportingDepartment(long id, [FromBody] OrganizationSupportingDepartmentUpdateDto request, CancellationToken cancellationToken)
        {
            var result = await _scope.SetOrganizationDepartmentAsync(id, request, cancellationToken);
            return Updated(result);
        }

        /// <summary>Removes the relationship without deleting the branch or the supporting department.</summary>
        [Permission(Permissions.SupportingDepartments.Update)]
        [HttpDelete("{id:long}/supporting-department")]
        public async Task<ActionResult<ApiResponse<object>>> RemoveSupportingDepartment(long id, CancellationToken cancellationToken)
        {
            await _scope.RemoveOrganizationDepartmentAsync(id, cancellationToken);
            return Done("ارتباط شعبه با دپارتمان پشتیبانی حذف شد.");
        }
    }
}
