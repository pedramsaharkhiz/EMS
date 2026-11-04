using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeographyCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Provinces and Cities had no uniqueness rule before this migration. Stop with a readable message (instead of a
            // bare "duplicate key" error from CREATE INDEX) when existing rows would violate the new rules; fix or deactivate
            // the listed duplicates and run the migration again.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Provinces] WHERE [IsActive] = 1 GROUP BY [Title] HAVING COUNT(*) > 1)
    THROW 51000, 'Provinces contains active rows with the same Title. Deactivate or rename the duplicates, then re-run the migration.', 1;
IF EXISTS (SELECT 1 FROM [Cities] WHERE [IsActive] = 1 GROUP BY [ProvinceId], [Title] HAVING COUNT(*) > 1)
    THROW 51001, 'Cities contains active rows with the same Title in the same province. Deactivate or rename the duplicates, then re-run the migration.', 1;");

            migrationBuilder.DropIndex(
                name: "IX_SupportingDepartments_CityId",
                table: "SupportingDepartments");

            migrationBuilder.DropIndex(
                name: "UX_SupportingDepartments_Title",
                table: "SupportingDepartments");

            migrationBuilder.DropIndex(
                name: "IX_Cities_ProvinceId",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "UX_SupportingDepartments_CityId_Title",
                table: "SupportingDepartments",
                columns: new[] { "CityId", "Title" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Provinces_Title",
                table: "Provinces",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Cities_ProvinceId_Title",
                table: "Cities",
                columns: new[] { "ProvinceId", "Title" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SupportingDepartments_CityId_Title",
                table: "SupportingDepartments");

            migrationBuilder.DropIndex(
                name: "UX_Provinces_Title",
                table: "Provinces");

            migrationBuilder.DropIndex(
                name: "UX_Cities_ProvinceId_Title",
                table: "Cities");

            migrationBuilder.CreateIndex(
                name: "IX_SupportingDepartments_CityId",
                table: "SupportingDepartments",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "UX_SupportingDepartments_Title",
                table: "SupportingDepartments",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_ProvinceId",
                table: "Cities",
                column: "ProvinceId");
        }
    }
}
