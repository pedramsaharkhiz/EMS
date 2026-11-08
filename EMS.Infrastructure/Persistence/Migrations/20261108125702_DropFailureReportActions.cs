using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropFailureReportActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FailureReportActions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FailureReportActions",
                columns: table => new
                {
                    FailureReportActionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionId = table.Column<long>(type: "bigint", nullable: false),
                    FailureReportId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ModifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FailureReportActions", x => x.FailureReportActionId);
                    table.ForeignKey(
                        name: "FK_FailureReportActions_ActionEntities_ActionId",
                        column: x => x.ActionId,
                        principalTable: "ActionEntities",
                        principalColumn: "ActionId");
                    table.ForeignKey(
                        name: "FK_FailureReportActions_FailureReports_FailureReportId",
                        column: x => x.FailureReportId,
                        principalTable: "FailureReports",
                        principalColumn: "FailureReportId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FailureReportActions_ActionId",
                table: "FailureReportActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_FailureReportActions_FailureReportId",
                table: "FailureReportActions",
                column: "FailureReportId");
        }
    }
}
