BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_SupportingDepartments_Title] ON [SupportingDepartments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_FailureCategories_Title] ON [FailureCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_EquipmentTypes_Title] ON [EquipmentTypes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_EquipmentStatuses_Title] ON [EquipmentStatuses];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_EquipmentModels_Title] ON [EquipmentModels];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_EquipmentLocationTypes_Title] ON [EquipmentLocationTypes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_EquipmentCategories_Title] ON [EquipmentCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    DROP INDEX [UX_Brands_Title] ON [Brands];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_SupportingDepartments_Title] ON [SupportingDepartments] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_FailureCategories_Title] ON [FailureCategories] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_EquipmentTypes_Title] ON [EquipmentTypes] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_EquipmentStatuses_Title] ON [EquipmentStatuses] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_EquipmentModels_Title] ON [EquipmentModels] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_EquipmentLocationTypes_Title] ON [EquipmentLocationTypes] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_EquipmentCategories_Title] ON [EquipmentCategories] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Brands_Title] ON [Brands] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102192841_CatalogIntegrity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102192841_CatalogIntegrity', N'10.0.14');
END;

COMMIT;
GO

