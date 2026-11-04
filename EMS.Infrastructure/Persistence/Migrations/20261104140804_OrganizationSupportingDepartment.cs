using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationSupportingDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SupportingDepartmentId",
                table: "Organizations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_SupportingDepartmentId",
                table: "Organizations",
                column: "SupportingDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_SupportingDepartments_SupportingDepartmentId",
                table: "Organizations",
                column: "SupportingDepartmentId",
                principalTable: "SupportingDepartments",
                principalColumn: "SupportingDepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_SupportingDepartments_SupportingDepartmentId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_SupportingDepartmentId",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SupportingDepartmentId",
                table: "Organizations");
        }
    }
}
