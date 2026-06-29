-- ============================================================================
-- Восстановление связей ProjectPhysicalVolumes → Kfo/Ber по кодам каталога
-- (CatalogCode, CatalogId, WorkId), если PhysicalVolumeId уже удалён.
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

UPDATE ppv
SET ppv.[PhysicalVolumeBerId] = ber.[Id]
FROM [ProjectPhysicalVolumes] ppv
INNER JOIN [PhysicalVolumesBer] ber ON ber.[Code] = ppv.[CatalogCode]
WHERE ppv.[PhysicalVolumeBerId] IS NULL
  AND ppv.[CatalogCode] IS NOT NULL
  AND LTRIM(RTRIM(ppv.[CatalogCode])) <> N'';
GO

UPDATE ppv
SET ppv.[PhysicalVolumeBerId] = ber.[Id]
FROM [ProjectPhysicalVolumes] ppv
INNER JOIN [PhysicalVolumesBer] ber ON ber.[Code] = ppv.[CatalogId]
WHERE ppv.[PhysicalVolumeBerId] IS NULL
  AND ppv.[CatalogId] IS NOT NULL
  AND LTRIM(RTRIM(ppv.[CatalogId])) <> N'';
GO

UPDATE ppv
SET ppv.[PhysicalVolumeBerId] = ber.[Id]
FROM [ProjectPhysicalVolumes] ppv
INNER JOIN [PhysicalVolumesBer] ber ON ber.[Code] = ppv.[WorkId]
WHERE ppv.[PhysicalVolumeBerId] IS NULL
  AND ppv.[WorkId] IS NOT NULL
  AND LTRIM(RTRIM(ppv.[WorkId])) <> N'';
GO

UPDATE ppv
SET ppv.[PhysicalVolumeKfoId] = kfo.[Id]
FROM [ProjectPhysicalVolumes] ppv
INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Code] = ppv.[CatalogCode]
WHERE ppv.[PhysicalVolumeKfoId] IS NULL
  AND ppv.[CatalogCode] IS NOT NULL
  AND LTRIM(RTRIM(ppv.[CatalogCode])) <> N'';
GO

UPDATE ppv
SET ppv.[PhysicalVolumeKfoId] = kfo.[Id]
FROM [ProjectPhysicalVolumes] ppv
INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Code] = ppv.[CatalogId]
WHERE ppv.[PhysicalVolumeKfoId] IS NULL
  AND ppv.[CatalogId] IS NOT NULL
  AND LTRIM(RTRIM(ppv.[CatalogId])) <> N'';
GO

UPDATE ppv
SET ppv.[PhysicalVolumeKfoId] = kfo.[Id]
FROM [ProjectPhysicalVolumes] ppv
INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Code] = ppv.[WorkId]
WHERE ppv.[PhysicalVolumeKfoId] IS NULL
  AND ppv.[WorkId] IS NOT NULL
  AND LTRIM(RTRIM(ppv.[WorkId])) <> N'';
GO
