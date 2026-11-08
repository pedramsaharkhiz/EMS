-- Drops the unused FailureReportActions table (it overlapped with TechnicianAssignmentActions and had no API). Back it up first if you want to keep any rows: SELECT * FROM FailureReportActions;
﻿BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108125702_DropFailureReportActions'
)
BEGIN
    DROP TABLE [FailureReportActions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108125702_DropFailureReportActions'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108125702_DropFailureReportActions', N'10.0.14');
END;

COMMIT;
GO

