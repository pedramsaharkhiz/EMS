using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentTitlePriorityViewed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriorityId",
                table: "TechnicianAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TechnicianAssignments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ViewedDateTime",
                table: "TechnicianAssignments",
                type: "datetime2",
                nullable: true);

            // Existing assignments: the priority of their report, and a title made from the id (both columns are required from now on).
            // The time of sight stays empty (nobody recorded it before).
            // EXEC: the columns above are added in the same batch of the idempotent script, so the statements must be compiled later.
            migrationBuilder.Sql(
                "EXEC(N'UPDATE a SET a.PriorityId = r.PriorityId " +
                "FROM TechnicianAssignments a INNER JOIN FailureReports r ON r.FailureReportId = a.FailureReportId')");
            migrationBuilder.Sql(
                "EXEC(N'UPDATE TechnicianAssignments SET Title = N''ارجاع شماره '' + CAST(TechnicianAssignmentId AS nvarchar(20)) WHERE Title = N''''')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorityId",
                table: "TechnicianAssignments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TechnicianAssignments");

            migrationBuilder.DropColumn(
                name: "ViewedDateTime",
                table: "TechnicianAssignments");
        }
    }
}
