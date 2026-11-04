using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FailureReportModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FailureReportStatusId",
                table: "FailureReports",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);   // existing reports (there is no API for them yet) become "Open"

            migrationBuilder.AddColumn<long>(
                name: "ReportedByUserId",
                table: "FailureReports",
                type: "bigint",
                nullable: true);

            // existing reports: the user who created the row, or else the first user (the column becomes required below)
            migrationBuilder.Sql(@"
UPDATE f SET ReportedByUserId = CASE
    WHEN EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.Id = f.CreatedByUserId) THEN f.CreatedByUserId
    ELSE (SELECT MIN(Id) FROM AspNetUsers) END
FROM FailureReports f");

            migrationBuilder.AlterColumn<long>(
                name: "ReportedByUserId",
                table: "FailureReports",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FailureReports_FailureReportStatusId",
                table: "FailureReports",
                column: "FailureReportStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_FailureReports_ReportedByUserId",
                table: "FailureReports",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FailureReports_ReportedDateTime",
                table: "FailureReports",
                column: "ReportedDateTime");

            migrationBuilder.CreateIndex(
                name: "UX_FailureReports_ReferenceNumber",
                table: "FailureReports",
                column: "ReferenceNumber",
                unique: true,
                filter: "[IsActive] = 1 AND [ReferenceNumber] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FailureReports_AspNetUsers_ReportedByUserId",
                table: "FailureReports",
                column: "ReportedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FailureReports_FailureReportStatuses_FailureReportStatusId",
                table: "FailureReports",
                column: "FailureReportStatusId",
                principalTable: "FailureReportStatuses",
                principalColumn: "FailureReportStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FailureReports_AspNetUsers_ReportedByUserId",
                table: "FailureReports");

            migrationBuilder.DropForeignKey(
                name: "FK_FailureReports_FailureReportStatuses_FailureReportStatusId",
                table: "FailureReports");

            migrationBuilder.DropIndex(
                name: "IX_FailureReports_FailureReportStatusId",
                table: "FailureReports");

            migrationBuilder.DropIndex(
                name: "IX_FailureReports_ReportedByUserId",
                table: "FailureReports");

            migrationBuilder.DropIndex(
                name: "IX_FailureReports_ReportedDateTime",
                table: "FailureReports");

            migrationBuilder.DropIndex(
                name: "UX_FailureReports_ReferenceNumber",
                table: "FailureReports");

            migrationBuilder.DropColumn(
                name: "FailureReportStatusId",
                table: "FailureReports");

            migrationBuilder.DropColumn(
                name: "ReportedByUserId",
                table: "FailureReports");
        }
    }
}
