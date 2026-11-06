-- Indexes on UserRefreshTokens: refresh and logout look a token up by its hash (a unique SHA-256, so the index is unique), and the clean-up job
-- finds expired / revoked tokens by date. Without them every refresh read the whole table.
--
-- Also: the equipment and failure-report duplicate checks (serial number, asset number, reference number) now compare the column with "=" so they can
-- use their unique indexes, relying on the column collation being case-insensitive ("a1" = "A1"). Check it once on the real database:
--
--   SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation');             -- the name should contain _CI_ (for example SQL_Latin1_General_CP1_CI_AS)
--   SELECT c.name, c.collation_name FROM sys.columns c
--    WHERE c.object_id = OBJECT_ID('Equipments') AND c.name IN ('SerialNumber', 'AssetNumber');
--
-- If it contains _CS_, tell the developer BEFORE deploying this version: the duplicate check would then become case-sensitive.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261106085601_RefreshTokenIndexes'
)
BEGIN
    CREATE INDEX [IX_UserRefreshTokens_ExpiresDateTime] ON [UserRefreshTokens] ([ExpiresDateTime]);

    CREATE INDEX [IX_UserRefreshTokens_RevokedDateTime] ON [UserRefreshTokens] ([RevokedDateTime]) WHERE [RevokedDateTime] IS NOT NULL;

    CREATE UNIQUE INDEX [UX_UserRefreshTokens_TokenHash] ON [UserRefreshTokens] ([TokenHash]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261106085601_RefreshTokenIndexes', N'10.0.14');
END;
COMMIT;
GO
