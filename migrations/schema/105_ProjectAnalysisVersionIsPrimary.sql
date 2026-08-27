-- 105: Основная версия анализа (отдельно для плана и факта)
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProjectAnalysisVersions') AND name = N'IsPrimary')
BEGIN
    ALTER TABLE dbo.ProjectAnalysisVersions
        ADD IsPrimary BIT NOT NULL
            CONSTRAINT DF_PAV_IsPrimary DEFAULT 0;
    PRINT 'Added IsPrimary';
END
GO

-- Backfill: одна основная на (ProjectId, Direction∈{Plan=1, Fact=2}) среди неархивных.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

;WITH Ranked AS (
    SELECT
        v.Id,
        ROW_NUMBER() OVER (
            PARTITION BY v.ProjectId, v.Direction
            ORDER BY v.SortOrder, v.Id
        ) AS Rn,
        MAX(CASE WHEN v.IsPrimary = 1 THEN 1 ELSE 0 END) OVER (
            PARTITION BY v.ProjectId, v.Direction
        ) AS HasPrimary
    FROM dbo.ProjectAnalysisVersions v
    WHERE v.IsArchived = 0
      AND v.Direction IN (1, 2)
)
UPDATE v
SET v.IsPrimary = 1,
    v.ModifiedAt = SYSUTCDATETIME()
FROM dbo.ProjectAnalysisVersions v
INNER JOIN Ranked r ON r.Id = v.Id
WHERE r.Rn = 1
  AND r.HasPrimary = 0;

PRINT '105_ProjectAnalysisVersionIsPrimary completed';
GO
