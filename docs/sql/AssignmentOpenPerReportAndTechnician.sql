-- A report may now have several open assignments (one per technician, for failures that need more than one technician): the unique index on
-- FailureReportId (open assignments only) is replaced by a unique index on (FailureReportId, TechnicianUserId) (open assignments only).
-- Safe on existing data: the old rule allowed at most one open assignment per report, so no pair can repeat.
-- Rolling back (Down) recreates the old index and FAILS if any report has more than one open assignment by then; cancel the extra ones first.
﻿BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111122501_AssignmentOpenPerReportAndTechnician'
)
BEGIN
    DROP INDEX [UX_TechnicianAssignments_OpenPerReport] ON [TechnicianAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111122501_AssignmentOpenPerReportAndTechnician'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UX_TechnicianAssignments_OpenPerReportAndTechnician] ON [TechnicianAssignments] ([FailureReportId], [TechnicianUserId]) WHERE [TechnicianAssignmentStatusId] IN (1, 2, 4)');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261111122501_AssignmentOpenPerReportAndTechnician'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261111122501_AssignmentOpenPerReportAndTechnician', N'10.0.14');
END;

COMMIT;
GO

