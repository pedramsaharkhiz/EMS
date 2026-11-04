using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedEquipmentStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "EquipmentStatuses",
                columns: new[] { "EquipmentStatusId", "Code", "CreatedByUserId", "CreatedDateTime", "IsActive", "ModifiedByUserId", "ModifiedDateTime", "Title" },
                values: new object[,]
                {
                    { 1L, "InStock", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "موجود در انبار" },
                    { 2L, "InUse", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "در حال استفاده" },
                    { 3L, "Assigned", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "تخصیص داده شده" },
                    { 4L, "UnderMaintenance", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "در تعمیر / سرویس" },
                    { 5L, "OutOfService", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "خارج از سرویس" },
                    { 6L, "Damaged", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "آسیب‌دیده" },
                    { 7L, "Lost", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "مفقود" },
                    { 8L, "Scrapped", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "اسقاط شده" },
                    { 9L, "Decommissioned", 0L, new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "از رده خارج" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                table: "EquipmentStatuses",
                keyColumn: "EquipmentStatusId",
                keyValue: 9L);
        }
    }
}
