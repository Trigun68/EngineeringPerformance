-- ============================================================================
-- Удаление устаревшей колонки [NormValue] из [IicWorkDirectionNorms].
-- Значение дублировало [Performance]; в UI и логике используется только Performance.
-- Идемпотентно.
-- ============================================================================

IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON c.object_id = t.object_id
    WHERE t.name = N'IicWorkDirectionNorms'
      AND c.name = N'NormValue'
)
BEGIN
    ALTER TABLE [IicWorkDirectionNorms] DROP COLUMN [NormValue];
    PRINT N'Dropped column [NormValue] from [IicWorkDirectionNorms].';
END
ELSE
BEGIN
    PRINT N'Column [NormValue] not found on [IicWorkDirectionNorms]; skip.';
END;
