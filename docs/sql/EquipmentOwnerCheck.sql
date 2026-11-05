-- Equipments.Owner: existing rows got the default 0 from AddEquipmentOwner, which is not an owner. They become Company (1) (a business decision),
-- then a CHECK constraint makes the table refuse any owner that is not 1 (Company) or 2 (Bank).
--
-- Before running (optional): how many rows will change?
--   SELECT COUNT(*) FROM Equipments WHERE Owner NOT IN (1, 2);
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105135723_EquipmentOwnerCheck'
)
BEGIN
    UPDATE [Equipments] SET [Owner] = 1 WHERE [Owner] NOT IN (1, 2);

    ALTER TABLE [Equipments] ADD CONSTRAINT [CK_Equipments_Owner] CHECK ([Owner] IN (1, 2));

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105135723_EquipmentOwnerCheck', N'10.0.14');
END;

COMMIT;
GO
