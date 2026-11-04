using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AttachmentContents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EntityAttachments_TableId",
                table: "EntityAttachments");

            migrationBuilder.CreateTable(
                name: "AttachmentContents",
                columns: table => new
                {
                    StreamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "varbinary(32)", nullable: true),
                    Content = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttachmentContents", x => x.StreamId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttachments_StreamId",
                table: "EntityAttachments",
                column: "StreamId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttachments_Table_Entity_Active",
                table: "EntityAttachments",
                columns: new[] { "TableId", "EntityId" },
                filter: "[IsActive] = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_EntityAttachments_AttachmentContents_StreamId",
                table: "EntityAttachments",
                column: "StreamId",
                principalTable: "AttachmentContents",
                principalColumn: "StreamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EntityAttachments_AttachmentContents_StreamId",
                table: "EntityAttachments");

            migrationBuilder.DropTable(
                name: "AttachmentContents");

            migrationBuilder.DropIndex(
                name: "IX_EntityAttachments_StreamId",
                table: "EntityAttachments");

            migrationBuilder.DropIndex(
                name: "IX_EntityAttachments_Table_Entity_Active",
                table: "EntityAttachments");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAttachments_TableId",
                table: "EntityAttachments",
                column: "TableId");
        }
    }
}
