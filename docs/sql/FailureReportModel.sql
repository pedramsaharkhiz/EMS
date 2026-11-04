-- FailureReports gets a status (FailureReportStatuses), the user who registered it, optimistic concurrency (already on
-- ModifiedDateTime, no column), a filtered unique index on ReferenceNumber and an index on ReportedDateTime.
-- Run FailureCatalogs.sql first (it creates FailureReportStatuses with the row Open = 1).
--
-- Before running on a database that already has reports (there was no API for them, so normally none):
--   SELECT ReferenceNumber, COUNT(*) FROM FailureReports WHERE IsActive = 1 AND ReferenceNumber IS NOT NULL GROUP BY ReferenceNumber HAVING COUNT(*) > 1;
-- Duplicates make the unique index fail. Existing reports become "Open" and are attributed to the user that created the row
-- (or else the first user).
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104115424_FailureReportModel'
)
BEGIN
    ALTER TABLE [FailureReports] ADD [FailureReportStatusId] bigint NOT NULL DEFAULT CAST(1 AS bigint);
    ALTER TABLE [FailureReports] ADD [ReportedByUserId] bigint NULL;

    EXEC(N'
UPDATE f SET ReportedByUserId = CASE
    WHEN EXISTS (SELECT 1 FROM AspNetUsers u WHERE u.Id = f.CreatedByUserId) THEN f.CreatedByUserId
    ELSE (SELECT MIN(Id) FROM AspNetUsers) END
FROM FailureReports f');

    ALTER TABLE [FailureReports] ALTER COLUMN [ReportedByUserId] bigint NOT NULL;

    CREATE INDEX [IX_FailureReports_FailureReportStatusId] ON [FailureReports] ([FailureReportStatusId]);
    CREATE INDEX [IX_FailureReports_ReportedByUserId] ON [FailureReports] ([ReportedByUserId]);
    CREATE INDEX [IX_FailureReports_ReportedDateTime] ON [FailureReports] ([ReportedDateTime]);
    CREATE UNIQUE INDEX [UX_FailureReports_ReferenceNumber] ON [FailureReports] ([ReferenceNumber]) WHERE [IsActive] = 1 AND [ReferenceNumber] IS NOT NULL;

    ALTER TABLE [FailureReports] ADD CONSTRAINT [FK_FailureReports_AspNetUsers_ReportedByUserId] FOREIGN KEY ([ReportedByUserId]) REFERENCES [AspNetUsers] ([Id]);
    ALTER TABLE [FailureReports] ADD CONSTRAINT [FK_FailureReports_FailureReportStatuses_FailureReportStatusId] FOREIGN KEY ([FailureReportStatusId]) REFERENCES [FailureReportStatuses] ([FailureReportStatusId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104115424_FailureReportModel', N'10.0.14');
END;

COMMIT;
GO
