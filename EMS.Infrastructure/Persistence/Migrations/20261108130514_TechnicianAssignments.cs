using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TechnicianAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TechnicianAssignments_FailureReportId",
                table: "TechnicianAssignments");

            migrationBuilder.DropIndex(
                name: "IX_TechnicianAssignments_TechnicianUserId",
                table: "TechnicianAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_TechnicianAssignmentActions_TechnicianAssignmentId",
                table: "TechnicianAssignmentActions",
                newName: "IX_TechnicianAssignmentActions_Assignment");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TechnicianAssignmentStatuses",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ResultNote",
                table: "TechnicianAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "EquipmentLocationHistoryId",
                table: "TechnicianAssignmentActions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.InsertData(
                table: "TechnicianAssignmentStatuses",
                columns: new[] { "TechnicianAssignmentStatusId", "Code", "CreatedByUserId", "CreatedDateTime", "IsActive", "ModifiedByUserId", "ModifiedDateTime", "Title" },
                values: new object[,]
                {
                    { 1L, "Assigned", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "ارجاع‌شده" },
                    { 2L, "Accepted", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "پذیرفته‌شده" },
                    { 3L, "Rejected", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "ردشده" },
                    { 4L, "InProgress", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "در حال انجام" },
                    { 5L, "Completed", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "تکمیل‌شده" },
                    { 6L, "Cancelled", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "لغوشده" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_TechnicianAssignmentStatuses_Code",
                table: "TechnicianAssignmentStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_Technician_Status",
                table: "TechnicianAssignments",
                columns: new[] { "TechnicianUserId", "TechnicianAssignmentStatusId" });

            migrationBuilder.CreateIndex(
                name: "UX_TechnicianAssignments_OpenPerReport",
                table: "TechnicianAssignments",
                column: "FailureReportId",
                unique: true,
                filter: "[TechnicianAssignmentStatusId] IN (1, 2, 4)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TechnicianAssignmentStatuses_Code",
                table: "TechnicianAssignmentStatuses");

            migrationBuilder.DropIndex(
                name: "IX_TechnicianAssignments_Technician_Status",
                table: "TechnicianAssignments");

            migrationBuilder.DropIndex(
                name: "UX_TechnicianAssignments_OpenPerReport",
                table: "TechnicianAssignments");

            migrationBuilder.DeleteData(
                table: "TechnicianAssignmentStatuses",
                keyColumn: "TechnicianAssignmentStatusId",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "TechnicianAssignmentStatuses",
                keyColumn: "TechnicianAssignmentStatusId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "TechnicianAssignmentStatuses",
                keyColumn: "TechnicianAssignmentStatusId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "TechnicianAssignmentStatuses",
                keyColumn: "TechnicianAssignmentStatusId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "TechnicianAssignmentStatuses",
                keyColumn: "TechnicianAssignmentStatusId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "TechnicianAssignmentStatuses",
                keyColumn: "TechnicianAssignmentStatusId",
                keyValue: 6L);

            migrationBuilder.DropColumn(
                name: "Code",
                table: "TechnicianAssignmentStatuses");

            migrationBuilder.DropColumn(
                name: "ResultNote",
                table: "TechnicianAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_TechnicianAssignmentActions_Assignment",
                table: "TechnicianAssignmentActions",
                newName: "IX_TechnicianAssignmentActions_TechnicianAssignmentId");

            migrationBuilder.AlterColumn<long>(
                name: "EquipmentLocationHistoryId",
                table: "TechnicianAssignmentActions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_FailureReportId",
                table: "TechnicianAssignments",
                column: "FailureReportId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianAssignments_TechnicianUserId",
                table: "TechnicianAssignments",
                column: "TechnicianUserId");
        }
    }
}
