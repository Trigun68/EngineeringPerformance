-- 122: уникальный индекс регистра стоимости работ.
-- Backfill, если 115 создал только таблицу (индекс был внутри IF NOT EXISTS таблицы).
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'IicWorkDirectionWorkCosts')
AND NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_IicWorkDirectionWorkCosts_WorkDirection_EffectiveFrom'
      AND object_id = OBJECT_ID(N'dbo.IicWorkDirectionWorkCosts'))
BEGIN
    CREATE UNIQUE INDEX [UX_IicWorkDirectionWorkCosts_WorkDirection_EffectiveFrom]
        ON [IicWorkDirectionWorkCosts] ([WorkDirectionId], [EffectiveFrom] DESC)
        INCLUDE ([EffectiveTo], [RatePerHour]);
END;

PRINT '122_EnsureWorkCostUniqueIndex completed';
