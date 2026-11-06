-- A branch (Organizations) has exactly one parent: a field (FieldId) OR a branch management (OrganizationManagementId),
-- never neither and never both (business decision, docs/BUSINESS-DECISIONS.md section 0).
--
-- Existing rows that break the rule are NOT changed by this script: which parent is right is a business question. The script stops
-- and the rows are corrected by hand first.
--
-- Before running: find the rows that would stop it (both queries must return nothing).
--   SELECT OrganizationId, Title FROM Organizations WHERE FieldId IS NULL AND OrganizationManagementId IS NULL;          -- give them a parent
--   SELECT OrganizationId, Title FROM Organizations WHERE FieldId IS NOT NULL AND OrganizationManagementId IS NOT NULL;  -- clear OrganizationManagementId
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106162913_OrganizationParentCheck'
)
BEGIN
    IF EXISTS (SELECT 1 FROM [Organizations] WHERE [FieldId] IS NULL AND [OrganizationManagementId] IS NULL)
        THROW 51010, 'Organizations contains rows with neither FieldId nor OrganizationManagementId. Give each such branch a field or a branch management, then re-run the script.', 1;
    IF EXISTS (SELECT 1 FROM [Organizations] WHERE [FieldId] IS NOT NULL AND [OrganizationManagementId] IS NOT NULL)
        THROW 51011, 'Organizations contains rows with both FieldId and OrganizationManagementId. Clear OrganizationManagementId when FieldId is set, then re-run the script.', 1;

    ALTER TABLE [Organizations] ADD CONSTRAINT [CK_Organizations_FieldOrManagement_ExactlyOne] CHECK (([FieldId] IS NOT NULL AND [OrganizationManagementId] IS NULL) OR ([FieldId] IS NULL AND [OrganizationManagementId] IS NOT NULL));

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106162913_OrganizationParentCheck', N'10.0.14');
END;

COMMIT;
GO
