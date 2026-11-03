-- Before running: the unique index UX_Skills_Title fails if two ACTIVE skills share a title. Check first:
--   SELECT Title, COUNT(*) FROM Skills WHERE IsActive = 1 GROUP BY Title HAVING COUNT(*) > 1;
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103110712_SkillCatalogIndexes'
)
BEGIN
    CREATE INDEX [IX_Skills_Code] ON [Skills] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103110712_SkillCatalogIndexes'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_Skills_Title] ON [Skills] ([Title]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103110712_SkillCatalogIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103110712_SkillCatalogIndexes', N'10.0.14');
END;

COMMIT;
GO

