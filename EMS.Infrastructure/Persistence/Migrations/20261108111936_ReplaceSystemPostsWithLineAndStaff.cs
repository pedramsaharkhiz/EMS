using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceSystemPostsWithLineAndStaff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The old posts (تکنسین، مسئول واحد، ثبت‌کننده) go away and the ids 1 and 2 get a new meaning (صف، ستاد): a user must not keep a post
            // that now means something else, and the foreign key would refuse the delete of the old row 3. Posts that administrators added
            // themselves (ids above 3) are not touched. See docs/adr/0017-retire-technician-post.md.
            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [PostId] = NULL WHERE [PostId] IN (1, 2, 3);");

            migrationBuilder.DeleteData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 3L);

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 1L,
                columns: new[] { "Code", "Title" },
                values: new object[] { "Line", "صف" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 2L,
                columns: new[] { "Code", "Title" },
                values: new object[] { "Staff", "ستاد" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 1L,
                columns: new[] { "Code", "Title" },
                values: new object[] { "Technician", "تکنسین" });

            migrationBuilder.UpdateData(
                table: "Posts",
                keyColumn: "PostId",
                keyValue: 2L,
                columns: new[] { "Code", "Title" },
                values: new object[] { "UnitManager", "مسئول واحد" });

            migrationBuilder.InsertData(
                table: "Posts",
                columns: new[] { "PostId", "Code", "CreatedByUserId", "CreatedDateTime", "IsActive", "ModifiedByUserId", "ModifiedDateTime", "Title" },
                values: new object[] { 3L, "Registrar", 0L, new DateTime(2026, 11, 4, 0, 0, 0, 0, DateTimeKind.Utc), true, null, null, "ثبت‌کننده" });
        }
    }
}
