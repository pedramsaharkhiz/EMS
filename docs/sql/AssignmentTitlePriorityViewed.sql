-- Technician assignments get a Title (required, 200), a PriorityId (required, 1 = کم .. 5 = فوری, the scale of a failure report) and a ViewedDateTime
-- (when the technician first saw it; empty = not yet).
-- Existing rows: the priority of their failure report, the title "ارجاع شماره <id>", and an empty ViewedDateTime. Safe to run on data.
-- Rolling back (Down) drops the three columns and loses their values.
﻿BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111124018_AssignmentTitlePriorityViewed'
)
BEGIN
    ALTER TABLE [TechnicianAssignments] ADD [PriorityId] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111124018_AssignmentTitlePriorityViewed'
)
BEGIN
    ALTER TABLE [TechnicianAssignments] ADD [Title] nvarchar(200) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111124018_AssignmentTitlePriorityViewed'
)
BEGIN
    ALTER TABLE [TechnicianAssignments] ADD [ViewedDateTime] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111124018_AssignmentTitlePriorityViewed'
)
BEGIN
    EXEC(N'UPDATE a SET a.PriorityId = r.PriorityId FROM TechnicianAssignments a INNER JOIN FailureReports r ON r.FailureReportId = a.FailureReportId')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111124018_AssignmentTitlePriorityViewed'
)
BEGIN
    EXEC(N'UPDATE TechnicianAssignments SET Title = N''ارجاع شماره '' + CAST(TechnicianAssignmentId AS nvarchar(20)) WHERE Title = N''''')
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111124018_AssignmentTitlePriorityViewed'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111124018_AssignmentTitlePriorityViewed', N'10.0.14');
END;

COMMIT;
GO

