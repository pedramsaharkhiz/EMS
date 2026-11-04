using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FailureCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FailureTypes_FailureCategoryId",
                table: "FailureTypes");

            migrationBuilder.CreateTable(
                name: "FailureReportStatuses",
                columns: table => new
                {
                    FailureReportStatusId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureReportStatuses", x => x.FailureReportStatusId);
                });

            migrationBuilder.InsertData(
                table: "FailureReportStatuses",
                columns: new[] { "FailureReportStatusId", "Code", "CreatedByUserId", "CreatedDateTime", "IsActive", "ModifiedByUserId", "ModifiedDateTime", "Title" },
                values: new object[,]
                {
                    { 1L, "Open", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "باز" },
                    { 2L, "InProgress", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "در حال رسیدگی" },
                    { 3L, "Closed", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "بسته" }
                });

            migrationBuilder.InsertData(
                table: "ReportTypes",
                columns: new[] { "ReportTypeId", "Code", "CreatedByUserId", "CreatedDateTime", "IsActive", "ModifiedByUserId", "ModifiedDateTime", "Title" },
                values: new object[,]
                {
                    { 1L, "Failure", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "خرابی" },
                    { 2L, "Incident", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "حادثه" },
                    { 3L, "Complaint", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "ثبت شکایت" },
                    { 4L, "Request", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "درخواست رسیدگی" },
                    { 5L, "MaintenanceNeed", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "نیاز به تعمیر / سرویس" },
                    { 6L, "InspectionResult", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "نتیجه‌ی بازرسی" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_ReportTypes_Title",
                table: "ReportTypes",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_FailureTypes_FailureCategoryId_Title",
                table: "FailureTypes",
                columns: new[] { "FailureCategoryId", "Title" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_ActionEntities_Title",
                table: "ActionEntities",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_FailureReportStatuses_Title",
                table: "FailureReportStatuses",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailureReportStatuses");

            migrationBuilder.DropIndex(
                name: "UX_ReportTypes_Title",
                table: "ReportTypes");

            migrationBuilder.DropIndex(
                name: "UX_FailureTypes_FailureCategoryId_Title",
                table: "FailureTypes");

            migrationBuilder.DropIndex(
                name: "UX_ActionEntities_Title",
                table: "ActionEntities");

            migrationBuilder.DeleteData(
                table: "ReportTypes",
                keyColumn: "ReportTypeId",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "ReportTypes",
                keyColumn: "ReportTypeId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "ReportTypes",
                keyColumn: "ReportTypeId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "ReportTypes",
                keyColumn: "ReportTypeId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "ReportTypes",
                keyColumn: "ReportTypeId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "ReportTypes",
                keyColumn: "ReportTypeId",
                keyValue: 6L);

            migrationBuilder.CreateIndex(
                name: "IX_FailureTypes_FailureCategoryId",
                table: "FailureTypes",
                column: "FailureCategoryId");
        }
    }
}
