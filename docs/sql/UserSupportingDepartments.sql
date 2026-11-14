-- A user may work for several supporting departments (and a department has many users): the column AspNetUsers.SupportingDepartmentId becomes the table
-- UserSupportingDepartments (UserId, SupportingDepartmentId), primary key on both. Every user keeps the department he had (copied before the column is dropped).
-- Safe to run on data. Rolling back (Down) puts back ONE department per user (the smallest id); the others are lost.
-- The API changes with it (supportingDepartmentId -> supportingDepartmentIds / supportingDepartments): deploy the new UI together.
Build started...
Build succeeded.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    CREATE TABLE [UserSupportingDepartments] (
        [UserId] bigint NOT NULL,
        [SupportingDepartmentId] bigint NOT NULL,
        CONSTRAINT [PK_UserSupportingDepartments] PRIMARY KEY ([UserId], [SupportingDepartmentId]),
        CONSTRAINT [FK_UserSupportingDepartments_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]),
        CONSTRAINT [FK_UserSupportingDepartments_SupportingDepartments_SupportingDepartmentId] FOREIGN KEY ([SupportingDepartmentId]) REFERENCES [SupportingDepartments] ([SupportingDepartmentId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    CREATE INDEX [IX_UserSupportingDepartments_SupportingDepartmentId] ON [UserSupportingDepartments] ([SupportingDepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    EXEC(N'INSERT INTO UserSupportingDepartments (UserId, SupportingDepartmentId) SELECT Id, SupportingDepartmentId FROM AspNetUsers WHERE SupportingDepartmentId IS NOT NULL')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    ALTER TABLE [AspNetUsers] DROP CONSTRAINT [FK_AspNetUsers_SupportingDepartments_SupportingDepartmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    DROP INDEX [IX_AspNetUsers_SupportingDepartmentId] ON [AspNetUsers];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[AspNetUsers]') AND [c].[name] = N'SupportingDepartmentId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [AspNetUsers] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [AspNetUsers] DROP COLUMN [SupportingDepartmentId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114085239_UserSupportingDepartments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114085239_UserSupportingDepartments', N'10.0.14');
END;

COMMIT;
GO


