using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Permission;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    public class PermissionController : BaseApiController
    {
        /// <summary>
        /// The catalog of every permission of the system, grouped by resource, with Persian titles and descriptions. It is the same for
        /// everyone and holds nothing secret, so any signed-in user may read it; what a ROLE holds is read from <c>GET /api/Role/{id}/permissions</c>.
        /// </summary>
        [Lookup]
        [HttpGet]
        public ActionResult<ApiResponse<IReadOnlyList<PermissionGroupDto>>> GetAll()
        {
            IReadOnlyList<PermissionGroupDto> groups = PermissionCatalog.Groups.Select(g => new PermissionGroupDto
            {
                Key = g.Key,
                Title = g.Title,
                Permissions = g.Permissions.Select(p => new PermissionItemDto { Name = p.Name, Action = p.Action, Title = p.Title, Description = p.Description }).ToList(),
            }).ToList();

            return Success(groups);
        }
    }
}
