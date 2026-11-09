using EMS.Application.Common.Models;
using EMS.Application.Common.Security;
using EMS.Application.Dtos.Dashboard;
using EMS.Application.Interfaces.Equipment;
using EMS.Application.Interfaces.FailureReport;
using EMS.Application.Interfaces.TechnicianAssignment;
using EMS.Presentation.Security;
using Microsoft.AspNetCore.Mvc;

namespace EMS.Presentation.Controllers
{
    /// <summary>
    /// Cards of the first page. Each card is its own endpoint with the permission of the data it counts, and is computed by the service of that
    /// module, from the very same rows its list shows, so whatever limits the list limits the numbers. Every number equals the <c>totalCount</c>
    /// of the matching list filter (see docs/DASHBOARD-DESIGN.md).
    /// </summary>
    public class DashboardController : BaseApiController
    {
        private readonly IEquipmentService _equipment;
        private readonly IFailureReportService _failureReports;
        private readonly ITechnicianAssignmentService _assignments;

        public DashboardController(IEquipmentService equipment, IFailureReportService failureReports, ITechnicianAssignmentService assignments)
        {
            _equipment = equipment;
            _failureReports = failureReports;
            _assignments = assignments;
        }

        /// <summary>"دارایی‌های ثبت‌شده": the active equipment per status and owner, and how many need action. Needs <c>Equipments.View</c>.</summary>
        [Permission(Permissions.Equipments.View)]
        [HttpGet("equipment")]
        public async Task<ActionResult<ApiResponse<EquipmentFleetSummaryDto>>> GetEquipment(CancellationToken cancellationToken) =>
            Success(await _equipment.GetFleetSummaryAsync(cancellationToken));

        /// <summary>Failure reports per status, the unresolved ones per priority, and those nobody was assigned. Needs <c>FailureReports.View</c>.</summary>
        [Permission(Permissions.FailureReports.View)]
        [HttpGet("failure-reports")]
        public async Task<ActionResult<ApiResponse<FailureReportDashboardDto>>> GetFailureReports(CancellationToken cancellationToken) =>
            Success(await _failureReports.GetDashboardAsync(cancellationToken));

        /// <summary>Technician assignments per status, with the open and the overdue ones. Needs <c>TechnicianAssignments.View</c>.</summary>
        [Permission(Permissions.TechnicianAssignments.View)]
        [HttpGet("assignments")]
        public async Task<ActionResult<ApiResponse<AssignmentDashboardDto>>> GetAssignments(CancellationToken cancellationToken) =>
            Success(await _assignments.GetDashboardAsync(cancellationToken));
    }
}
