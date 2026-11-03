using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentYaasCodeUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop with a readable message (instead of a bare "duplicate key" error from CREATE INDEX) when existing active
            // equipment share a Yaas code; deactivate or correct the duplicates and run the migration again.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Equipments] WHERE [IsActive] = 1 GROUP BY [YaasCode] HAVING COUNT(*) > 1)
    THROW 51002, 'Equipments contains active rows with the same YaasCode. Deactivate or correct the duplicates, then re-run the migration.', 1;");

            migrationBuilder.CreateIndex(
                name: "UX_Equipments_YaasCode",
                table: "Equipments",
                column: "YaasCode",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Equipments_YaasCode",
                table: "Equipments");
        }
    }
}
