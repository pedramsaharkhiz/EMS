using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentOpenPerReportAndTechnician : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TechnicianAssignments_OpenPerReport",
                table: "TechnicianAssignments");

            migrationBuilder.CreateIndex(
                name: "UX_TechnicianAssignments_OpenPerReportAndTechnician",
                table: "TechnicianAssignments",
                columns: new[] { "FailureReportId", "TechnicianUserId" },
                unique: true,
                filter: "[TechnicianAssignmentStatusId] IN (1, 2, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TechnicianAssignments_OpenPerReportAndTechnician",
                table: "TechnicianAssignments");

            migrationBuilder.CreateIndex(
                name: "UX_TechnicianAssignments_OpenPerReport",
                table: "TechnicianAssignments",
                column: "FailureReportId",
                unique: true,
                filter: "[TechnicianAssignmentStatusId] IN (1, 2, 4)");
        }
    }
}
