using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignmentNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NoteCategories",
                columns: table => new
                {
                    NoteCategoryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteCategories", x => x.NoteCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentNotes",
                columns: table => new
                {
                    AssignmentNoteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnicianAssignmentId = table.Column<long>(type: "bigint", nullable: false),
                    NoteCategoryId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SeenDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentNotes", x => x.AssignmentNoteId);
                    table.ForeignKey(
                        name: "FK_AssignmentNotes_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AssignmentNotes_NoteCategories_NoteCategoryId",
                        column: x => x.NoteCategoryId,
                        principalTable: "NoteCategories",
                        principalColumn: "NoteCategoryId");
                    table.ForeignKey(
                        name: "FK_AssignmentNotes_TechnicianAssignments_TechnicianAssignmentId",
                        column: x => x.TechnicianAssignmentId,
                        principalTable: "TechnicianAssignments",
                        principalColumn: "TechnicianAssignmentId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentNotes_CreatedByUserId",
                table: "AssignmentNotes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentNotes_NoteCategoryId",
                table: "AssignmentNotes",
                column: "NoteCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentNotes_TechnicianAssignmentId_CreatedDateTime_AssignmentNoteId",
                table: "AssignmentNotes",
                columns: new[] { "TechnicianAssignmentId", "CreatedDateTime", "AssignmentNoteId" });

            migrationBuilder.CreateIndex(
                name: "UX_NoteCategories_Title",
                table: "NoteCategories",
                column: "Title",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentNotes");

            migrationBuilder.DropTable(
                name: "NoteCategories");
        }
    }
}
