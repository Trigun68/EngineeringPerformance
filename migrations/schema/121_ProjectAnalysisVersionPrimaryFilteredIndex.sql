-- 121: одна основная (IsPrimary) неархивная версия на проект и направление.
-- Filtered unique index: не-primary и архивные версии не ограничиваются, поэтому
-- существующие данные (несколько архивных primary) индекс не ломает.
-- Зеркало EF: ProjectAnalysisVersionConfiguration.PrimaryVersionIndexName.
SET NOCOUNT ON;
-- Filtered index требует QUOTED_IDENTIFIER ON (sqlcmd без -I выставляет OFF).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM dbo.ProjectAnalysisVersions
    WHERE IsPrimary = 1 AND IsArchived = 0
    GROUP BY ProjectId, Direction
    HAVING COUNT(*) > 1)
BEGIN
    -- Данные нарушают инвариант: оставляем основной самую свежую (по Id) версию направления.
    ;WITH ranked AS (
        SELECT Id,
               ROW_NUMBER() OVER (PARTITION BY ProjectId, Direction ORDER BY Id DESC) AS rn
        FROM dbo.ProjectAnalysisVersions
        WHERE IsPrimary = 1 AND IsArchived = 0)
    UPDATE v SET IsPrimary = 0
    FROM dbo.ProjectAnalysisVersions v
    JOIN ranked r ON r.Id = v.Id
    WHERE r.rn > 1;
    PRINT 'Demoted duplicate primary versions (kept the newest per project/direction)';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ProjectAnalysisVersions')
      AND name = N'UX_ProjectAnalysisVersions_OnePrimary')
BEGIN
    CREATE UNIQUE INDEX [UX_ProjectAnalysisVersions_OnePrimary]
        ON dbo.ProjectAnalysisVersions ([ProjectId], [Direction])
        WHERE [IsPrimary] = 1 AND [IsArchived] = 0;
    PRINT 'Created UX_ProjectAnalysisVersions_OnePrimary';
END

COMMIT TRANSACTION;
PRINT '121_ProjectAnalysisVersionPrimaryFilteredIndex completed';
