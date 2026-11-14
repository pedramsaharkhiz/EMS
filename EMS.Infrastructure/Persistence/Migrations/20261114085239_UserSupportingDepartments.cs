using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UserSupportingDepartments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A user may work for several supporting departments: the single column becomes a link table.
            migrationBuilder.CreateTable(
                name: "UserSupportingDepartments",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SupportingDepartmentId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSupportingDepartments", x => new { x.UserId, x.SupportingDepartmentId });
                    table.ForeignKey(
                        name: "FK_UserSupportingDepartments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSupportingDepartments_SupportingDepartments_SupportingDepartmentId",
                        column: x => x.SupportingDepartmentId,
                        principalTable: "SupportingDepartments",
                        principalColumn: "SupportingDepartmentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSupportingDepartments_SupportingDepartmentId",
                table: "UserSupportingDepartments",
                column: "SupportingDepartmentId");

            // Every user keeps the department he had (this must run before the column goes).
            // EXEC: the table above is created in the same batch of the idempotent script, so the statement must be compiled later.
            migrationBuilder.Sql(
                "EXEC(N'INSERT INTO UserSupportingDepartments (UserId, SupportingDepartmentId) " +
                "SELECT Id, SupportingDepartmentId FROM AspNetUsers WHERE SupportingDepartmentId IS NOT NULL')");

            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_SupportingDepartments_SupportingDepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SupportingDepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SupportingDepartmentId",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SupportingDepartmentId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SupportingDepartmentId",
                table: "AspNetUsers",
                column: "SupportingDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_SupportingDepartments_SupportingDepartmentId",
                table: "AspNetUsers",
                column: "SupportingDepartmentId",
                principalTable: "SupportingDepartments",
                principalColumn: "SupportingDepartmentId");

            // The column holds ONE department: a user with several keeps the one with the smallest id (the others are lost).
            // EXEC: the column above is added in the same batch of the idempotent script.
            migrationBuilder.Sql(
                "EXEC(N'UPDATE u SET u.SupportingDepartmentId = m.SupportingDepartmentId " +
                "FROM AspNetUsers u INNER JOIN (SELECT UserId, MIN(SupportingDepartmentId) AS SupportingDepartmentId FROM UserSupportingDepartments GROUP BY UserId) m ON m.UserId = u.Id')");

            migrationBuilder.DropTable(
                name: "UserSupportingDepartments");
        }
    }
}
