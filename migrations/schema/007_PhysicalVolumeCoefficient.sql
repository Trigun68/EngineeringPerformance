-- ============================================================================
-- PhysicalVolumes: коэффициент перевода количества по проекту в единицы группы
-- физ. объёмов (ИИС). Значение по умолчанию для существующих строк: 1.
-- EF: 20260419213543_AddPhysicalVolumeCoefficient
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[PhysicalVolumes]')
    AND name = N'Coefficient'
)
BEGIN
    ALTER TABLE [PhysicalVolumes]
    ADD [Coefficient] decimal(18,4) NOT NULL CONSTRAINT [DF_PhysicalVolumes_Coefficient] DEFAULT (1);
    PRINT 'Added column Coefficient to PhysicalVolumes (default 1)';
END
ELSE
BEGIN
    PRINT 'Column Coefficient already exists in PhysicalVolumes';
END
GO

-- ============================================================================
-- Record migration in __EFMigrationsHistory
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260419213543_AddPhysicalVolumeCoefficient'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260419213543_AddPhysicalVolumeCoefficient', N'10.0.5');
END
GO

PRINT 'Migration AddPhysicalVolumeCoefficient (SQL) completed.'
GO
