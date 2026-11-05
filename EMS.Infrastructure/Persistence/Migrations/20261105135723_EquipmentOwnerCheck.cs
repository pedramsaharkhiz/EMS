using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentOwnerCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // AddEquipmentOwner added the column with the default 0, which is not an owner: every existing equipment becomes
            // "Company" (1) (a business decision), then the table is made to refuse anything but the defined owners.
            migrationBuilder.Sql("UPDATE [Equipments] SET [Owner] = 1 WHERE [Owner] NOT IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_Owner",
                table: "Equipments",
                sql: "[Owner] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_Owner",
                table: "Equipments");
        }
    }
}
