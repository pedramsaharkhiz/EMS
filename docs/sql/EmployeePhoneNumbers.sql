BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114133547_EmployeePhoneNumbers'
)
BEGIN
    CREATE TABLE [EmployeePhoneNumbers] (
        [EmployeePhoneNumberId] bigint NOT NULL IDENTITY,
        [EmployeeId] bigint NOT NULL,
        [PhoneNumber] varchar(11) NOT NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedByUserId] bigint NOT NULL,
        [CreatedDateTime] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedByUserId] bigint NULL,
        [ModifiedDateTime] datetime2 NULL,
        CONSTRAINT [PK_EmployeePhoneNumbers] PRIMARY KEY ([EmployeePhoneNumberId]),
        CONSTRAINT [CK_EmployeePhoneNumbers_Mobile] CHECK (LEN([PhoneNumber]) = 11 AND [PhoneNumber] LIKE '09%' AND [PhoneNumber] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'),
        CONSTRAINT [FK_EmployeePhoneNumbers_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([EmployeeId])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114133547_EmployeePhoneNumbers'
)
BEGIN
    CREATE INDEX [IX_EmployeePhoneNumbers_EmployeeId] ON [EmployeePhoneNumbers] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114133547_EmployeePhoneNumbers'
)
BEGIN
    CREATE UNIQUE INDEX [UX_EmployeePhoneNumbers_PhoneNumber] ON [EmployeePhoneNumbers] ([PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261114133547_EmployeePhoneNumbers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261114133547_EmployeePhoneNumbers', N'10.0.14');
END;

COMMIT;
GO

