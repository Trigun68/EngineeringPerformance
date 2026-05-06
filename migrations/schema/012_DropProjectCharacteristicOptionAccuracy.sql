-- ============================================================================
-- Schema 012: Drop ProjectCharacteristicOptions.ComparisonAccuracy
--   Точность сравнения теперь вычисляется в коде по значению PD%
--   (см. EngineeringPerformance.Core.ComparisonAccuracy.FromValuePercent).
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429223256_DropProjectCharacteristicOptionAccuracy'
)
BEGIN
    IF COL_LENGTH('ProjectCharacteristicOptions', 'ComparisonAccuracy') IS NOT NULL
    BEGIN
        ALTER TABLE [ProjectCharacteristicOptions] DROP COLUMN [ComparisonAccuracy];
    END;

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429223256_DropProjectCharacteristicOptionAccuracy', N'10.0.5');
END;
COMMIT;
GO
