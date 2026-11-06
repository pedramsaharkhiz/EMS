using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_ExpiresDateTime",
                table: "UserRefreshTokens",
                column: "ExpiresDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_RevokedDateTime",
                table: "UserRefreshTokens",
                column: "RevokedDateTime",
                filter: "[RevokedDateTime] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_UserRefreshTokens_TokenHash",
                table: "UserRefreshTokens",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_ExpiresDateTime",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_RevokedDateTime",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "UX_UserRefreshTokens_TokenHash",
                table: "UserRefreshTokens");
        }
    }
}
