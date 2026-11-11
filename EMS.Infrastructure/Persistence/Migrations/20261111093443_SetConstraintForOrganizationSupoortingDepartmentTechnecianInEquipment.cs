using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetConstraintForOrganizationSupoortingDepartmentTechnecianInEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_Technician_RequiresDepartment",
                table: "Equipments",
                sql: "[TechnicianUserId] IS NULL OR [CurrentSupportingDepartmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_Technician_RequiresDepartment",
                table: "Equipments");
        }
    }
}
