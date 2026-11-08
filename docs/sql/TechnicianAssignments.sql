-- Technician assignments: status codes + the six system statuses (ids 1-6), ResultNote, the "one open assignment per report" unique index and
-- EquipmentLocationHistoryId of an action becomes optional.
--
-- Run this on a database where TechnicianAssignmentStatuses is EMPTY (nothing wrote to it before this version: it had no API or seed).
-- Check first:  SELECT COUNT(*) FROM TechnicianAssignmentStatuses;   -- must be 0, otherwise tell the developer BEFORE running.
-- It also drops the old non-unique index on FailureReportId, which the new filtered unique index replaces. If TechnicianAssignments already
-- holds rows, check that no report has two assignments before running (the new index would refuse them):
--   SELECT FailureReportId, COUNT(*) FROM TechnicianAssignments GROUP BY FailureReportId HAVING COUNT(*) > 1;
﻿BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    DROP INDEX [IX_TechnicianAssignments_FailureReportId] ON [TechnicianAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    DROP INDEX [IX_TechnicianAssignments_TechnicianUserId] ON [TechnicianAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    EXEC sp_rename N'[TechnicianAssignmentActions].[IX_TechnicianAssignmentActions_TechnicianAssignmentId]', N'IX_TechnicianAssignmentActions_Assignment', 'INDEX';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    ALTER TABLE [TechnicianAssignmentStatuses] ADD [Code] varchar(20) NOT NULL DEFAULT '';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    ALTER TABLE [TechnicianAssignments] ADD [ResultNote] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TechnicianAssignmentActions]') AND [c].[name] = N'EquipmentLocationHistoryId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [TechnicianAssignmentActions] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [TechnicianAssignmentActions] ALTER COLUMN [EquipmentLocationHistoryId] bigint NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'TechnicianAssignmentStatusId', N'Code', N'CreatedByUserId', N'CreatedDateTime', N'IsActive', N'ModifiedByUserId', N'ModifiedDateTime', N'Title') AND [object_id] = OBJECT_ID(N'[TechnicianAssignmentStatuses]'))
        SET IDENTITY_INSERT [TechnicianAssignmentStatuses] ON;
    EXEC(N'INSERT INTO [TechnicianAssignmentStatuses] ([TechnicianAssignmentStatusId], [Code], [CreatedByUserId], [CreatedDateTime], [IsActive], [ModifiedByUserId], [ModifiedDateTime], [Title])
    VALUES (CAST(1 AS bigint), ''Assigned'', CAST(0 AS bigint), ''2026-11-04T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''ارجاع‌شده''),
    (CAST(2 AS bigint), ''Accepted'', CAST(0 AS bigint), ''2026-11-04T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''پذیرفته‌شده''),
    (CAST(3 AS bigint), ''Rejected'', CAST(0 AS bigint), ''2026-11-04T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''ردشده''),
    (CAST(4 AS bigint), ''InProgress'', CAST(0 AS bigint), ''2026-11-04T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''در حال انجام''),
    (CAST(5 AS bigint), ''Completed'', CAST(0 AS bigint), ''2026-11-04T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''تکمیل‌شده''),
    (CAST(6 AS bigint), ''Cancelled'', CAST(0 AS bigint), ''2026-11-04T00:00:00.0000000Z'', CAST(1 AS bit), NULL, NULL, N''لغوشده'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'TechnicianAssignmentStatusId', N'Code', N'CreatedByUserId', N'CreatedDateTime', N'IsActive', N'ModifiedByUserId', N'ModifiedDateTime', N'Title') AND [object_id] = OBJECT_ID(N'[TechnicianAssignmentStatuses]'))
        SET IDENTITY_INSERT [TechnicianAssignmentStatuses] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    CREATE UNIQUE INDEX [UX_TechnicianAssignmentStatuses_Code] ON [TechnicianAssignmentStatuses] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    CREATE INDEX [IX_TechnicianAssignments_Technician_Status] ON [TechnicianAssignments] ([TechnicianUserId], [TechnicianAssignmentStatusId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TechnicianAssignments_OpenPerReport] ON [TechnicianAssignments] ([FailureReportId]) WHERE [TechnicianAssignmentStatusId] IN (1, 2, 4)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108130514_TechnicianAssignments'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108130514_TechnicianAssignments', N'10.0.14');
END;

COMMIT;
GO

