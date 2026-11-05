-- The Yaas code stops being unique (several equipment may share one). The serial number and the asset number become unique among ALL
-- equipment (active or not, ignoring case); equipment without one (NULL) do not collide.
--
-- BEFORE running, list the values that are held by more than one equipment. Each row below makes the unique index fail until the duplicates are
-- corrected (changed or cleared) by the business. The script first trims values and turns blank ones into NULL, so check AFTER that clean-up:
--
--   UPDATE Equipments SET SerialNumber = NULLIF(LTRIM(RTRIM(SerialNumber)), '') WHERE SerialNumber IS NOT NULL;
--   UPDATE Equipments SET AssetNumber  = NULLIF(LTRIM(RTRIM(AssetNumber)),  '') WHERE AssetNumber  IS NOT NULL;
--   SELECT SerialNumber, COUNT(*) AS Equipments, STRING_AGG(CAST(EquipmentId AS varchar(20)), ', ') AS Ids
--     FROM Equipments WHERE SerialNumber IS NOT NULL GROUP BY SerialNumber HAVING COUNT(*) > 1;
--   SELECT AssetNumber, COUNT(*) AS Equipments, STRING_AGG(CAST(EquipmentId AS varchar(20)), ', ') AS Ids
--     FROM Equipments WHERE AssetNumber IS NOT NULL GROUP BY AssetNumber HAVING COUNT(*) > 1;
--
-- (The comparison follows the column collation, which is case-insensitive by default: "a1" and "A1" are the same value.)
BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261105140436_EquipmentSerialAssetUnique'
)
BEGIN
    DROP INDEX [UX_Equipments_YaasCode] ON [Equipments];

    UPDATE [Equipments] SET [SerialNumber] = NULLIF(LTRIM(RTRIM([SerialNumber])), '') WHERE [SerialNumber] IS NOT NULL;
    UPDATE [Equipments] SET [AssetNumber] = NULLIF(LTRIM(RTRIM([AssetNumber])), '') WHERE [AssetNumber] IS NOT NULL;

    CREATE UNIQUE INDEX [UX_Equipments_AssetNumber] ON [Equipments] ([AssetNumber]) WHERE [AssetNumber] IS NOT NULL;
    CREATE UNIQUE INDEX [UX_Equipments_SerialNumber] ON [Equipments] ([SerialNumber]) WHERE [SerialNumber] IS NOT NULL;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261105140436_EquipmentSerialAssetUnique', N'10.0.14');
END;

COMMIT;
GO
