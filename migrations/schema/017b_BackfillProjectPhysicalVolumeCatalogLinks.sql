-- ============================================================================
-- Восстановление связей ProjectPhysicalVolumes → Kfo/Ber по кодам каталога
-- (CatalogCode, CatalogId, WorkId), если PhysicalVolumeId уже удалён.
-- На fresh-DB колонка CatalogCode может отсутствовать — охраняем COL_LENGTH.
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogCode') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesBer')
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeBerId] = ber.[Id]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumesBer] ber ON ber.[Code] = ppv.[CatalogCode]
    WHERE ppv.[PhysicalVolumeBerId] IS NULL
      AND ppv.[CatalogCode] IS NOT NULL
      AND LTRIM(RTRIM(ppv.[CatalogCode])) <> N'''';
    ');
END;
GO

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesBer')
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeBerId] = ber.[Id]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumesBer] ber ON ber.[Code] = ppv.[CatalogId]
    WHERE ppv.[PhysicalVolumeBerId] IS NULL
      AND ppv.[CatalogId] IS NOT NULL
      AND LTRIM(RTRIM(ppv.[CatalogId])) <> N'''';
    ');
END;
GO

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'WorkId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesBer')
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeBerId] = ber.[Id]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumesBer] ber ON ber.[Code] = ppv.[WorkId]
    WHERE ppv.[PhysicalVolumeBerId] IS NULL
      AND ppv.[WorkId] IS NOT NULL
      AND LTRIM(RTRIM(ppv.[WorkId])) <> N'''';
    ');
END;
GO

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogCode') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesKfo')
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeKfoId] = kfo.[Id]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Code] = ppv.[CatalogCode]
    WHERE ppv.[PhysicalVolumeKfoId] IS NULL
      AND ppv.[CatalogCode] IS NOT NULL
      AND LTRIM(RTRIM(ppv.[CatalogCode])) <> N'''';
    ');
END;
GO

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesKfo')
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeKfoId] = kfo.[Id]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Code] = ppv.[CatalogId]
    WHERE ppv.[PhysicalVolumeKfoId] IS NULL
      AND ppv.[CatalogId] IS NOT NULL
      AND LTRIM(RTRIM(ppv.[CatalogId])) <> N'''';
    ');
END;
GO

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'WorkId') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesKfo')
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeKfoId] = kfo.[Id]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumesKfo] kfo ON kfo.[Code] = ppv.[WorkId]
    WHERE ppv.[PhysicalVolumeKfoId] IS NULL
      AND ppv.[WorkId] IS NOT NULL
      AND LTRIM(RTRIM(ppv.[WorkId])) <> N'''';
    ');
END;
GO
