using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SupportingDepartments_Title",
                table: "SupportingDepartments");

            migrationBuilder.DropIndex(
                name: "UX_FailureCategories_Title",
                table: "FailureCategories");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentTypes_Title",
                table: "EquipmentTypes");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentStatuses_Title",
                table: "EquipmentStatuses");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentModels_Title",
                table: "EquipmentModels");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentLocationTypes_Title",
                table: "EquipmentLocationTypes");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentCategories_Title",
                table: "EquipmentCategories");

            migrationBuilder.DropIndex(
                name: "UX_Brands_Title",
                table: "Brands");

            migrationBuilder.CreateIndex(
                name: "UX_SupportingDepartments_Title",
                table: "SupportingDepartments",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_FailureCategories_Title",
                table: "FailureCategories",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentTypes_Title",
                table: "EquipmentTypes",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentStatuses_Title",
                table: "EquipmentStatuses",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentModels_Title",
                table: "EquipmentModels",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentLocationTypes_Title",
                table: "EquipmentLocationTypes",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentCategories_Title",
                table: "EquipmentCategories",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "UX_Brands_Title",
                table: "Brands",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_SupportingDepartments_Title",
                table: "SupportingDepartments");

            migrationBuilder.DropIndex(
                name: "UX_FailureCategories_Title",
                table: "FailureCategories");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentTypes_Title",
                table: "EquipmentTypes");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentStatuses_Title",
                table: "EquipmentStatuses");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentModels_Title",
                table: "EquipmentModels");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentLocationTypes_Title",
                table: "EquipmentLocationTypes");

            migrationBuilder.DropIndex(
                name: "UX_EquipmentCategories_Title",
                table: "EquipmentCategories");

            migrationBuilder.DropIndex(
                name: "UX_Brands_Title",
                table: "Brands");

            migrationBuilder.CreateIndex(
                name: "UX_SupportingDepartments_Title",
                table: "SupportingDepartments",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_FailureCategories_Title",
                table: "FailureCategories",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentTypes_Title",
                table: "EquipmentTypes",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentStatuses_Title",
                table: "EquipmentStatuses",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentModels_Title",
                table: "EquipmentModels",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentLocationTypes_Title",
                table: "EquipmentLocationTypes",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentCategories_Title",
                table: "EquipmentCategories",
                column: "Title",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Brands_Title",
                table: "Brands",
                column: "Title",
                unique: true);
        }
    }
}
