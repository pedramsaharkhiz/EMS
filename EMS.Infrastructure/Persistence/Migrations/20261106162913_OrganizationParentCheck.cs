using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrganizationParentCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A branch has exactly one parent: a field OR a branch management (business decision, docs/BUSINESS-DECISIONS.md section 0).
            // Existing rows that break the rule are NOT fixed silently (which parent is right is a business question): the migration
            // stops and names the rows, they are corrected by hand, then the migration is run again.
            //   - no parent at all:   set FieldId or OrganizationManagementId.
            //   - both parents set:   keep FieldId and clear OrganizationManagementId (the management is reached through the field).
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM [Organizations] WHERE [FieldId] IS NULL AND [OrganizationManagementId] IS NULL)
    THROW 51010, 'Organizations contains rows with neither FieldId nor OrganizationManagementId. Give each such branch a field or a branch management, then re-run the migration. Find them: SELECT OrganizationId, Title FROM Organizations WHERE FieldId IS NULL AND OrganizationManagementId IS NULL', 1;
IF EXISTS (SELECT 1 FROM [Organizations] WHERE [FieldId] IS NOT NULL AND [OrganizationManagementId] IS NOT NULL)
    THROW 51011, 'Organizations contains rows with both FieldId and OrganizationManagementId. A branch has exactly one parent: clear OrganizationManagementId when FieldId is set, then re-run the migration. Find them: SELECT OrganizationId, Title FROM Organizations WHERE FieldId IS NOT NULL AND OrganizationManagementId IS NOT NULL', 1;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Organizations_FieldOrManagement_ExactlyOne",
                table: "Organizations",
                sql: "([FieldId] IS NOT NULL AND [OrganizationManagementId] IS NULL) OR ([FieldId] IS NULL AND [OrganizationManagementId] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Organizations_FieldOrManagement_ExactlyOne",
                table: "Organizations");
        }
    }
}
