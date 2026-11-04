-- Failure catalogs: unique-title indexes on FailureTypes (per category), ReportTypes and ActionEntities, the new table
-- FailureReportStatuses, and the system rows of ReportTypes (ids 1..6) and FailureReportStatuses (ids 1..3).
--
-- Before running, on a database that already has data in these tables (there was no API for them before, so normally empty):
--   SELECT FailureCategoryId, Title, COUNT(*) FROM FailureTypes WHERE IsActive = 1 GROUP BY FailureCategoryId, Title HAVING COUNT(*) > 1;
--   SELECT Title, COUNT(*) FROM ReportTypes    WHERE IsActive = 1 GROUP BY Title HAVING COUNT(*) > 1;
--   SELECT Title, COUNT(*) FROM ActionEntities WHERE IsActive = 1 GROUP BY Title HAVING COUNT(*) > 1;
--   SELECT COUNT(*) FROM ReportTypes WHERE ReportTypeId BETWEEN 1 AND 6;   -- expected 0 (the script inserts these ids)
-- Duplicates make the unique indexes fail; existing ReportTypes rows with ids 1..6 make the insert fail.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104105249_FailureCatalogs'
)
BEGIN
    DROP INDEX [IX_FailureTypes_FailureCategoryId] ON [FailureTypes];

    CREATE TABLE [FailureReportStatuses] (
        [FailureReportStatusId] bigint NOT NULL IDENTITY,
        [Title] nvarchar(100) NOT NULL,
        [Code] varchar(20) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedByUserId] bigint NOT NULL,
        [CreatedDateTime] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedByUserId] bigint NULL,
        [ModifiedDateTime] datetime2 NULL,
        CONSTRAINT [PK_FailureReportStatuses] PRIMARY KEY ([FailureReportStatusId])
    );

    SET IDENTITY_INSERT [FailureReportStatuses] ON;
    INSERT INTO [FailureReportStatuses] ([FailureReportStatusId], [Code], [CreatedByUserId], [CreatedDateTime], [IsActive], [ModifiedByUserId], [ModifiedDateTime], [Title])
    VALUES (1, 'Open', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'باز'),
           (2, 'InProgress', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'در حال رسیدگی'),
           (3, 'Closed', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'بسته');
    SET IDENTITY_INSERT [FailureReportStatuses] OFF;

    SET IDENTITY_INSERT [ReportTypes] ON;
    INSERT INTO [ReportTypes] ([ReportTypeId], [Code], [CreatedByUserId], [CreatedDateTime], [IsActive], [ModifiedByUserId], [ModifiedDateTime], [Title])
    VALUES (1, 'Failure', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'خرابی'),
           (2, 'Incident', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'حادثه'),
           (3, 'Complaint', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'ثبت شکایت'),
           (4, 'Request', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'درخواست رسیدگی'),
           (5, 'MaintenanceNeed', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'نیاز به تعمیر / سرویس'),
           (6, 'InspectionResult', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'نتیجه‌ی بازرسی');
    SET IDENTITY_INSERT [ReportTypes] OFF;

    CREATE UNIQUE INDEX [UX_ReportTypes_Title] ON [ReportTypes] ([Title]) WHERE [IsActive] = 1;
    CREATE UNIQUE INDEX [UX_FailureTypes_FailureCategoryId_Title] ON [FailureTypes] ([FailureCategoryId], [Title]) WHERE [IsActive] = 1;
    CREATE UNIQUE INDEX [UX_ActionEntities_Title] ON [ActionEntities] ([Title]) WHERE [IsActive] = 1;
    CREATE UNIQUE INDEX [UX_FailureReportStatuses_Title] ON [FailureReportStatuses] ([Title]) WHERE [IsActive] = 1;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104105249_FailureCatalogs', N'10.0.14');
END;

COMMIT;
GO
