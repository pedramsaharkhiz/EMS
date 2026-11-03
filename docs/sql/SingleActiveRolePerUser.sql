-- Before running: the unique index fails if a user has more than one ACTIVE role. Find them:
--   SELECT UserId, COUNT(*) AS ActiveRoles FROM AspNetUserRoles WHERE IsActive = 1 GROUP BY UserId HAVING COUNT(*) > 1;
-- Fix them (set IsActive = 0 on the extra rows, or remove the rows) and run this script afterwards.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103114441_SingleActiveRolePerUser'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_AspNetUserRoles_UserId_Active] ON [AspNetUserRoles] ([UserId]) WHERE [IsActive] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103114441_SingleActiveRolePerUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261103114441_SingleActiveRolePerUser', N'10.0.14');
END;

COMMIT;
GO

