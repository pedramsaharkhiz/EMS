using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PostId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    PostId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.PostId);
                });

            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "PostId", "Code", "CreatedByUserId", "CreatedDateTime", "IsActive", "ModifiedByUserId", "ModifiedDateTime", "Title" },
                values: new object[,]
                {
                    { 1L, "Technician", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "تکنسین" },
                    { 2L, "UnitManager", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "مسئول واحد" },
                    { 3L, "Registrar", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "ثبت‌کننده" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_PostId",
                table: "AspNetUsers",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "UX_Posts_Title",
                table: "Posts",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Posts_PostId",
                table: "AspNetUsers",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "PostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Posts_PostId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_PostId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "AspNetUsers");
        }
    }
}
