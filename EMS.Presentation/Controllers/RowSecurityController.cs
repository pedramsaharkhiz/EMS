using EMS.Application.Common.DataAccess;
using EMS.Application.Common.Models;
using EMS.Application.Dtos.Permission;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    /// <summary>Row-level security: how much of the data the signed-in user sees and why (docs/RLS-FINDINGS.md, ADR-0028).</summary>
    public class RowSecurityController : BaseApiController
    {
        private readonly IDataScopeAccessor _scope;
        private readonly IRowAccess _access;

        public RowSecurityController(IDataScopeAccessor scope, IRowAccess access)
        {
            _scope = scope;
            _access = access;
        }

        /// <summary>
        /// The data scope of the signed-in user: unrestricted or not, his department, the reason in words, and every protected type with the rule that applies to it.
        /// Any signed-in user may read his OWN scope; it reveals nothing about anyone else.
        /// </summary>
        [Lookup]
        [HttpGet("my-scope")]
        public ActionResult<ApiResponse<MyDataScopeDto>> GetMyScope()
        {
            var scope = _scope.Current;
            return Success(new MyDataScopeDto
            {
                UserId = scope.UserId,
                IsUnrestricted = scope.IsUnrestricted,
                SupportingDepartmentId = scope.SupportingDepartmentId,
                Reason = scope.Reason,
                Policies = _access.Describe()
                    .Select(p => new RowPolicyItemDto { Name = p.Name, RowType = p.RowType, Description = p.Description, RestrictsYou = p.RestrictsThisScope })
                    .ToList(),
            });
        }
    }
}
