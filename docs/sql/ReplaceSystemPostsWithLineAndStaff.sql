-- The system posts become two: Line (صف, id 1) and Staff (ستاد, id 2). The old تکنسین / مسئول واحد / ثبت‌کننده (ids 1, 2, 3) go away and the ids 1 and 2
-- get the new meaning; id 3 is deleted. See docs/adr/0017-retire-technician-post.md.
--
-- THIS CLEARS the post of every user who has one of the three old posts (their meaning is not the new one; the ids are reused). Posts that
-- administrators added themselves (ids above 3) and the users who hold them are not touched.
-- BEFORE running on a database that has data:
--   1. Take a backup.
--   2. An ACTIVE post added by an administrator with the title «صف» or «ستاد» would clash with the unique index of active titles: the script fails.
--      List them first and rename or deactivate them:
--        SELECT PostId, Title, Code, IsActive FROM Posts WHERE PostId > 3 AND Title IN (N'صف', N'ستاد');
--   3. To see whom the script will clear:
--        SELECT Id, UserName, PostId FROM AspNetUsers WHERE PostId IN (1, 2, 3);
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261108111936_ReplaceSystemPostsWithLineAndStaff'
)
BEGIN
    UPDATE [AspNetUsers] SET [PostId] = NULL WHERE [PostId] IN (1, 2, 3);

    DELETE FROM [Posts]
    WHERE [PostId] = CAST(3 AS bigint);

    UPDATE [Posts] SET [Code] = 'Line', [Title] = N'صف'
    WHERE [PostId] = CAST(1 AS bigint);

    UPDATE [Posts] SET [Code] = 'Staff', [Title] = N'ستاد'
    WHERE [PostId] = CAST(2 AS bigint);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261108111936_ReplaceSystemPostsWithLineAndStaff', N'10.0.14');
END;
COMMIT;
GO
