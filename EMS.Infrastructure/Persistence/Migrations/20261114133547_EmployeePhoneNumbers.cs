using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmployeePhoneNumbers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeePhoneNumbers",
                columns: table => new
                {
                    EmployeePhoneNumberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(11)", unicode: false, maxLength: 11, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePhoneNumbers", x => x.EmployeePhoneNumberId);
                    table.CheckConstraint("CK_EmployeePhoneNumbers_Mobile", "LEN([PhoneNumber]) = 11 AND [PhoneNumber] LIKE '09%' AND [PhoneNumber] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'");
                    table.ForeignKey(
                        name: "FK_EmployeePhoneNumbers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "EmployeeId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePhoneNumbers_EmployeeId",
                table: "EmployeePhoneNumbers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeePhoneNumbers_PhoneNumber",
                table: "EmployeePhoneNumbers",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeePhoneNumbers");
        }
    }
}
