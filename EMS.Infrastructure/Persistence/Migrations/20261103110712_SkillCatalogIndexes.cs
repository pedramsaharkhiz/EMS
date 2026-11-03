using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkillCatalogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.CreateIndex(
            //    name: "IX_Skills_Code",
            //    table: "Skills",
            //    column: "Code");

            //migrationBuilder.CreateIndex(
            //    name: "UX_Skills_Title",
            //    table: "Skills",
            //    column: "Title",
            //    unique: true,
            //    filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropIndex(
            //    name: "IX_Skills_Code",
            //    table: "Skills");

            //migrationBuilder.DropIndex(
            //    name: "UX_Skills_Title",
            //    table: "Skills");
        }
    }
}
