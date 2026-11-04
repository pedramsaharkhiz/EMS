-- Adds the post (سمت) catalog: table Posts with its three system rows, and the optional AspNetUsers.PostId.
-- No data conversion is needed: existing users simply have no post (PostId = NULL).
-- The three system rows (ids 1..3) are also guaranteed at every application start (SystemPostsGuard), so this script and the guard agree.
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104093000_AddPosts'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD [PostId] bigint NULL;

    CREATE TABLE [Posts] (
        [PostId] bigint NOT NULL IDENTITY,
        [Title] nvarchar(100) NOT NULL,
        [Code] varchar(20) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedByUserId] bigint NOT NULL,
        [CreatedDateTime] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
        [ModifiedByUserId] bigint NULL,
        [ModifiedDateTime] datetime2 NULL,
        CONSTRAINT [PK_Posts] PRIMARY KEY ([PostId])
    );

    SET IDENTITY_INSERT [Posts] ON;
    INSERT INTO [Posts] ([PostId], [Code], [CreatedByUserId], [CreatedDateTime], [IsActive], [ModifiedByUserId], [ModifiedDateTime], [Title])
    VALUES (1, 'Technician', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'تکنسین'),
           (2, 'UnitManager', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'مسئول واحد'),
           (3, 'Registrar', 0, '2026-11-04T00:00:00.0000000Z', CAST(1 AS bit), NULL, NULL, N'ثبت‌کننده');
    SET IDENTITY_INSERT [Posts] OFF;

    CREATE INDEX [IX_AspNetUsers_PostId] ON [AspNetUsers] ([PostId]);
    CREATE UNIQUE INDEX [UX_Posts_Title] ON [Posts] ([Title]) WHERE [IsActive] = 1;

    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Posts_PostId] FOREIGN KEY ([PostId]) REFERENCES [Posts] ([PostId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104093000_AddPosts', N'10.0.14');
END;

COMMIT;
GO
