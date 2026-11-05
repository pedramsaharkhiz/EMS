using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentSerialAssetUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Equipments_YaasCode",
                table: "Equipments");

            // Make old data say what the API stores: values are trimmed and a blank value is "none" (NULL). Then two equipment with the same
            // serial or asset number (ignoring case) make the unique indexes below fail: see docs/sql/EquipmentSerialAssetUnique.sql for the
            // queries that list them before running this.
            migrationBuilder.Sql("UPDATE [Equipments] SET [SerialNumber] = NULLIF(LTRIM(RTRIM([SerialNumber])), '') WHERE [SerialNumber] IS NOT NULL");
            migrationBuilder.Sql("UPDATE [Equipments] SET [AssetNumber] = NULLIF(LTRIM(RTRIM([AssetNumber])), '') WHERE [AssetNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Equipments_AssetNumber",
                table: "Equipments",
                column: "AssetNumber",
                unique: true,
                filter: "[AssetNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Equipments_SerialNumber",
                table: "Equipments",
                column: "SerialNumber",
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Equipments_AssetNumber",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "UX_Equipments_SerialNumber",
                table: "Equipments");

            migrationBuilder.CreateIndex(
                name: "UX_Equipments_YaasCode",
                table: "Equipments",
                column: "YaasCode",
                unique: true,
                filter: "[IsActive] = 1");
        }
    }
}
