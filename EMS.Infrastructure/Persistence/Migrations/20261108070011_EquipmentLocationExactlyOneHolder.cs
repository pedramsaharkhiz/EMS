using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EquipmentLocationExactlyOneHolder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentLocationHistories_EquipmentLocationTypes_EquipmentLocationTypeId",
                table: "EquipmentLocationHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipments_EquipmentLocationTypes_CurrentEquipmentLocationTypeId",
                table: "Equipments");

            migrationBuilder.DropTable(
                name: "EquipmentLocationTypes");

            migrationBuilder.DropIndex(
                name: "IX_Equipments_CurrentEquipmentLocationTypeId",
                table: "Equipments");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentLocationHistories_EquipmentLocationTypeId",
                table: "EquipmentLocationHistories");

            migrationBuilder.DropColumn(
                name: "CurrentEquipmentLocationTypeId",
                table: "Equipments");

            migrationBuilder.DropColumn(
                name: "EquipmentLocationTypeId",
                table: "EquipmentLocationHistories");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipments_Location_ExactlyOne",
                table: "Equipments",
                sql: "([CurrentOrganizationId] IS NOT NULL AND [CurrentSupportingDepartmentId] IS NULL) OR ([CurrentOrganizationId] IS NULL AND [CurrentSupportingDepartmentId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EquipmentLocationHistories_Location_ExactlyOne",
                table: "EquipmentLocationHistories",
                sql: "([OrganizationId] IS NOT NULL AND [SupportingDepartmentId] IS NULL) OR ([OrganizationId] IS NULL AND [SupportingDepartmentId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipments_Location_ExactlyOne",
                table: "Equipments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EquipmentLocationHistories_Location_ExactlyOne",
                table: "EquipmentLocationHistories");

            migrationBuilder.AddColumn<long>(
                name: "CurrentEquipmentLocationTypeId",
                table: "Equipments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "EquipmentLocationTypeId",
                table: "EquipmentLocationHistories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "EquipmentLocationTypes",
                columns: table => new
                {
                    EquipmentLocationTypeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentLocationTypes", x => x.EquipmentLocationTypeId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipments_CurrentEquipmentLocationTypeId",
                table: "Equipments",
                column: "CurrentEquipmentLocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentLocationHistories_EquipmentLocationTypeId",
                table: "EquipmentLocationHistories",
                column: "EquipmentLocationTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_EquipmentLocationTypes_Title",
                table: "EquipmentLocationTypes",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentLocationHistories_EquipmentLocationTypes_EquipmentLocationTypeId",
                table: "EquipmentLocationHistories",
                column: "EquipmentLocationTypeId",
                principalTable: "EquipmentLocationTypes",
                principalColumn: "EquipmentLocationTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipments_EquipmentLocationTypes_CurrentEquipmentLocationTypeId",
                table: "Equipments",
                column: "CurrentEquipmentLocationTypeId",
                principalTable: "EquipmentLocationTypes",
                principalColumn: "EquipmentLocationTypeId");
        }
    }
}
