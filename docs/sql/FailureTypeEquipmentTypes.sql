-- A failure type can be linked to the equipment types it applies to: the new table FailureTypeEquipmentTypes (FailureTypeId, EquipmentTypeId), primary key on both.
-- A failure type with NO row applies to every equipment type, so every existing failure type keeps working as before. Nothing is copied or changed in existing data.
-- Rolling back (Down) drops the table and loses the links.
Build started...
Build succeeded.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114105842_AddconflictFlagInequipment'
)
BEGIN
    ALTER TABLE [Equipments] ADD [HasConflict] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114105842_AddconflictFlagInequipment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114105842_AddconflictFlagInequipment', N'10.0.14');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114111542_FailureTypeEquipmentTypes'
)
BEGIN
    CREATE TABLE [FailureTypeEquipmentTypes] (
        [FailureTypeId] bigint NOT NULL,
        [EquipmentTypeId] bigint NOT NULL,
        CONSTRAINT [PK_FailureTypeEquipmentTypes] PRIMARY KEY ([FailureTypeId], [EquipmentTypeId]),
        CONSTRAINT [FK_FailureTypeEquipmentTypes_EquipmentTypes_EquipmentTypeId] FOREIGN KEY ([EquipmentTypeId]) REFERENCES [EquipmentTypes] ([EquipmentTypeId]),
        CONSTRAINT [FK_FailureTypeEquipmentTypes_FailureTypes_FailureTypeId] FOREIGN KEY ([FailureTypeId]) REFERENCES [FailureTypes] ([FailureTypeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114111542_FailureTypeEquipmentTypes'
)
BEGIN
    CREATE INDEX [IX_FailureTypeEquipmentTypes_EquipmentTypeId] ON [FailureTypeEquipmentTypes] ([EquipmentTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114111542_FailureTypeEquipmentTypes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114111542_FailureTypeEquipmentTypes', N'10.0.14');
END;

COMMIT;
GO


