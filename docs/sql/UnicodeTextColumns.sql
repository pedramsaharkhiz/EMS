BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103105616_UnicodeTextColumns'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tables]') AND [c].[name] = N'DisplayName');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tables] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Tables] ALTER COLUMN [DisplayName] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103105616_UnicodeTextColumns'
)
BEGIN
    DROP INDEX [RoleNameIndex] ON [AspNetRoles];
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetRoles]') AND [c].[name] = N'NormalizedName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [AspNetRoles] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [AspNetRoles] ALTER COLUMN [NormalizedName] nvarchar(256) NULL;
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103105616_UnicodeTextColumns'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetRoles]') AND [c].[name] = N'Name');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [AspNetRoles] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [AspNetRoles] ALTER COLUMN [Name] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103105616_UnicodeTextColumns'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103105616_UnicodeTextColumns', N'10.0.14');
END;

COMMIT;
GO

