-- ============================================================================
-- Schema 103: Link PhysicalVolumesBer → PhysicalVolumesKfo (номенклатура БЭР→КФО)
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.PhysicalVolumesBer', 'PhysicalVolumeKfoId') IS NULL
    ALTER TABLE [PhysicalVolumesBer] ADD [PhysicalVolumeKfoId] int NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhysicalVolumesBer_PhysicalVolumesKfo_PhysicalVolumeKfoId')
BEGIN
    ALTER TABLE [PhysicalVolumesBer] ADD CONSTRAINT [FK_PhysicalVolumesBer_PhysicalVolumesKfo_PhysicalVolumeKfoId]
        FOREIGN KEY ([PhysicalVolumeKfoId]) REFERENCES [PhysicalVolumesKfo] ([Id]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhysicalVolumesBer_PhysicalVolumeKfoId' AND object_id = OBJECT_ID(N'[dbo].[PhysicalVolumesBer]'))
    CREATE INDEX [IX_PhysicalVolumesBer_PhysicalVolumeKfoId] ON [PhysicalVolumesBer]([PhysicalVolumeKfoId]);
GO
