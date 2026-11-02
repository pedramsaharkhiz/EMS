BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102183227_AddAuditLog'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [TableName] nvarchar(128) NOT NULL,
        [EntityId] nvarchar(256) NOT NULL,
        [Action] varchar(16) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [ChangedColumns] nvarchar(max) NULL,
        [UserId] bigint NULL,
        [UserName] nvarchar(256) NULL,
        [IpAddress] varchar(45) NULL,
        [CorrelationId] varchar(64) NULL,
        [TimestampUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102183227_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_CorrelationId] ON [AuditLogs] ([CorrelationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102183227_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TableName_EntityId] ON [AuditLogs] ([TableName], [EntityId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102183227_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TimestampUtc] ON [AuditLogs] ([TimestampUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102183227_AddAuditLog'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261102183227_AddAuditLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261102183227_AddAuditLog', N'10.0.14');
END;

COMMIT;
GO

