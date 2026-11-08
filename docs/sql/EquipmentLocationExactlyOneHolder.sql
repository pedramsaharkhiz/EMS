-- An equipment is with a branch OR with a supporting department (ستاد): exactly one of Equipments.CurrentOrganizationId and
-- Equipments.CurrentSupportingDepartmentId is filled (CHECK constraint), and the same for EquipmentLocationHistories. The location-type
-- catalog (EquipmentLocationTypes) and its two foreign key columns are DROPPED: the type is now derived (branch / ستاد). See ADR-0014.
--
-- THIS DESTROYS the location types. The project was in early development with no data when it was written, so nothing is converted.
-- BEFORE running on a database that has data:
--   1. Take a backup.
--   2. The CHECK constraints fail on rows that do not satisfy them. List them first and fix them (choose one of the two columns):
--        SELECT EquipmentId, CurrentOrganizationId, CurrentSupportingDepartmentId FROM Equipments
--         WHERE (CurrentOrganizationId IS NULL AND CurrentSupportingDepartmentId IS NULL)
--            OR (CurrentOrganizationId IS NOT NULL AND CurrentSupportingDepartmentId IS NOT NULL);
--        SELECT EquipmentLocationHistoryId, EquipmentId, OrganizationId, SupportingDepartmentId FROM EquipmentLocationHistories
--         WHERE (OrganizationId IS NULL AND SupportingDepartmentId IS NULL)
--            OR (OrganizationId IS NOT NULL AND SupportingDepartmentId IS NOT NULL);
--   3. If the location types meant more than branch / ستاد (a warehouse, a repair shop, "with a technician"), that information is lost:
--        SELECT lt.Title, COUNT(*) FROM Equipments e JOIN EquipmentLocationTypes lt ON lt.EquipmentLocationTypeId = e.CurrentEquipmentLocationTypeId GROUP BY lt.Title;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108070011_EquipmentLocationExactlyOneHolder'
)
BEGIN
    ALTER TABLE [EquipmentLocationHistories] DROP CONSTRAINT [FK_EquipmentLocationHistories_EquipmentLocationTypes_EquipmentLocationTypeId];

    ALTER TABLE [Equipments] DROP CONSTRAINT [FK_Equipments_EquipmentLocationTypes_CurrentEquipmentLocationTypeId];

    DROP TABLE [EquipmentLocationTypes];

    DROP INDEX [IX_Equipments_CurrentEquipmentLocationTypeId] ON [Equipments];

    DROP INDEX [IX_EquipmentLocationHistories_EquipmentLocationTypeId] ON [EquipmentLocationHistories];

    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Equipments]') AND [c].[name] = N'CurrentEquipmentLocationTypeId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Equipments] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Equipments] DROP COLUMN [CurrentEquipmentLocationTypeId];

    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EquipmentLocationHistories]') AND [c].[name] = N'EquipmentLocationTypeId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [EquipmentLocationHistories] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [EquipmentLocationHistories] DROP COLUMN [EquipmentLocationTypeId];

    ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_Location_ExactlyOne] CHECK (([CurrentOrganizationId] IS NOT NULL AND [CurrentSupportingDepartmentId] IS NULL) OR ([CurrentOrganizationId] IS NULL AND [CurrentSupportingDepartmentId] IS NOT NULL));

    ALTER TABLE [EquipmentLocationHistories] ADD CONSTRAINT [CK_EquipmentLocationHistories_Location_ExactlyOne] CHECK (([OrganizationId] IS NOT NULL AND [SupportingDepartmentId] IS NULL) OR ([OrganizationId] IS NULL AND [SupportingDepartmentId] IS NOT NULL));

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108070011_EquipmentLocationExactlyOneHolder', N'10.0.14');
END;
COMMIT;
GO
