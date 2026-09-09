-- ============================================================================
-- 026: WorkDirectionId строк факта ← IicWorkDirectionId каталога трудоёмкости.
-- После 025 (группы IIC в справочнике) импортированные ProjectLaborIntensities
-- держали старое направление. Сопоставление: LaborIntensityId, иначе тип
-- документа (слева от '|') = Name / DocumentTypeName листа каталога.
-- Пустой IIC в каталоге → WorkDirectionId = NULL. Уникальный индекс
-- (Version, Project, DocumentationCode, WorkDirectionId) WHERE WD IS NOT NULL
-- обходится: не пишем, если соседняя строка уже занимает целевой ключ.
-- Идемпотентно. На существующей БД db-init пропускает, пока
-- FORCE_NOMENCLATURE_REFRESH=1 (вместе с 025).
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.ProjectLaborIntensities', N'U') IS NULL
   OR OBJECT_ID(N'dbo.LaborIntensities', N'U') IS NULL
BEGIN
    PRINT '026 skipped: ProjectLaborIntensities or LaborIntensities missing';
END
ELSE
BEGIN
BEGIN TRANSACTION;

;WITH Cat AS (
    SELECT li.Id, li.Name, li.DocumentTypeName, li.IicWorkDirectionId
    FROM dbo.LaborIntensities AS li
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.LaborIntensities AS c WHERE c.ParentId = li.Id
    )
),
Resolved AS (
    SELECT
        pli.Id,
        COALESCE(byId.Id, byName.Id) AS CatalogId,
        COALESCE(byId.IicWorkDirectionId, byName.IicWorkDirectionId) AS NewWd
    FROM dbo.ProjectLaborIntensities AS pli
    LEFT JOIN dbo.LaborIntensities AS byId ON byId.Id = pli.LaborIntensityId
    OUTER APPLY (
        SELECT TOP (1) c.Id, c.IicWorkDirectionId
        FROM Cat AS c
        WHERE pli.LaborIntensityId IS NULL
          AND (
              c.Name = CASE
                  WHEN CHARINDEX(N'|', pli.DocumentationCode) > 0
                  THEN LEFT(pli.DocumentationCode, CHARINDEX(N'|', pli.DocumentationCode) - 1)
                  ELSE pli.DocumentationCode
              END
              OR c.DocumentTypeName = CASE
                  WHEN CHARINDEX(N'|', pli.DocumentationCode) > 0
                  THEN LEFT(pli.DocumentationCode, CHARINDEX(N'|', pli.DocumentationCode) - 1)
                  ELSE pli.DocumentationCode
              END
          )
        ORDER BY CASE WHEN c.Name = CASE
                  WHEN CHARINDEX(N'|', pli.DocumentationCode) > 0
                  THEN LEFT(pli.DocumentationCode, CHARINDEX(N'|', pli.DocumentationCode) - 1)
                  ELSE pli.DocumentationCode
              END THEN 0 ELSE 1 END, c.Id
    ) AS byName
)
UPDATE pli
SET pli.WorkDirectionId = r.NewWd,
    pli.LaborIntensityId = COALESCE(pli.LaborIntensityId, r.CatalogId),
    pli.ModifiedAt = SYSUTCDATETIME()
FROM dbo.ProjectLaborIntensities AS pli
INNER JOIN Resolved AS r ON r.Id = pli.Id
WHERE r.CatalogId IS NOT NULL
  AND (
      ISNULL(pli.WorkDirectionId, -1) <> ISNULL(r.NewWd, -1)
      OR (pli.LaborIntensityId IS NULL AND r.CatalogId IS NOT NULL)
  )
  AND (
      r.NewWd IS NULL
      OR NOT EXISTS (
          SELECT 1
          FROM dbo.ProjectLaborIntensities AS other
          WHERE other.Id <> pli.Id
            AND other.ProjectAnalysisVersionId = pli.ProjectAnalysisVersionId
            AND other.ProjectId = pli.ProjectId
            AND other.DocumentationCode = pli.DocumentationCode
            AND other.WorkDirectionId = r.NewWd
      )
  );

DECLARE @Updated int = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT CONCAT(N'026 PLI WorkDirectionId from catalog: updated=', @Updated);
END;
