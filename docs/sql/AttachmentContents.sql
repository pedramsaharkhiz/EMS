-- Before running: the new foreign key EntityAttachments.StreamId -> AttachmentContents fails if EntityAttachments already has a row with a StreamId.
--   SELECT COUNT(*) FROM EntityAttachments WHERE StreamId IS NOT NULL;   -- expected 0 (there was no API for attachments before)
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104081244_AttachmentContents'
)
BEGIN
    DROP INDEX [IX_EntityAttachments_TableId] ON [EntityAttachments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104081244_AttachmentContents'
)
BEGIN
    CREATE TABLE [AttachmentContents] (
        [StreamId] uniqueidentifier NOT NULL,
        [FileName] nvarchar(255) NOT NULL,
        [ContentType] varchar(100) NOT NULL,
        [SizeBytes] bigint NOT NULL,
        [Sha256] varbinary(32) NULL,
        [Content] varbinary(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedByUserId] bigint NOT NULL,
        [CreatedDateTime] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedByUserId] bigint NULL,
        [ModifiedDateTime] datetime2 NULL,
        CONSTRAINT [PK_AttachmentContents] PRIMARY KEY ([StreamId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104081244_AttachmentContents'
)
BEGIN
    CREATE INDEX [IX_EntityAttachments_StreamId] ON [EntityAttachments] ([StreamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104081244_AttachmentContents'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_EntityAttachments_Table_Entity_Active] ON [EntityAttachments] ([TableId], [EntityId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104081244_AttachmentContents'
)
BEGIN
    ALTER TABLE [EntityAttachments] ADD CONSTRAINT [FK_EntityAttachments_AttachmentContents_StreamId] FOREIGN KEY ([StreamId]) REFERENCES [AttachmentContents] ([StreamId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104081244_AttachmentContents'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104081244_AttachmentContents', N'10.0.14');
END;

COMMIT;
GO

