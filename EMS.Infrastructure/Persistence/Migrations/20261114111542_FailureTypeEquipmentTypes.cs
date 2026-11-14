using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FailureTypeEquipmentTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailureTypeEquipmentTypes",
                columns: table => new
                {
                    FailureTypeId = table.Column<long>(type: "bigint", nullable: false),
                    EquipmentTypeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureTypeEquipmentTypes", x => new { x.FailureTypeId, x.EquipmentTypeId });
                    table.ForeignKey(
                        name: "FK_FailureTypeEquipmentTypes_EquipmentTypes_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EquipmentTypes",
                        principalColumn: "EquipmentTypeId");
                    table.ForeignKey(
                        name: "FK_FailureTypeEquipmentTypes_FailureTypes_FailureTypeId",
                        column: x => x.FailureTypeId,
                        principalTable: "FailureTypes",
                        principalColumn: "FailureTypeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailureTypeEquipmentTypes_EquipmentTypeId",
                table: "FailureTypeEquipmentTypes",
                column: "EquipmentTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailureTypeEquipmentTypes");
        }
    }
}
