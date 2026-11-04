-- A branch (Organization) can be served by one supporting department: Organizations.SupportingDepartmentId (nullable).
-- No data conversion: existing branches have no department until one is assigned through the API.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104140804_OrganizationSupportingDepartment'
)
BEGIN
    ALTER TABLE [Organizations] ADD [SupportingDepartmentId] bigint NULL;

    CREATE INDEX [IX_Organizations_SupportingDepartmentId] ON [Organizations] ([SupportingDepartmentId]);

    ALTER TABLE [Organizations] ADD CONSTRAINT [FK_Organizations_SupportingDepartments_SupportingDepartmentId] FOREIGN KEY ([SupportingDepartmentId]) REFERENCES [SupportingDepartments] ([SupportingDepartmentId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104140804_OrganizationSupportingDepartment', N'10.0.14');
END;

COMMIT;
GO
