-- ============================================================================
-- 025: группы IIC справочника трудоёмкости по выгрузке Excel
--   «справочник_трудоемкость.xlsx» (лист «Трудоёмкость», 2026-09-08).
-- Листья: Name LIKE '{Prefix}[_]%'. Пустое направление в Excel → NULL.
-- L2-родители: единогласный IIC детей. L1 не трогаем. Каскад на факт —
-- отдельный 026_SyncProjectLaborWorkDirectionFromCatalog.sql.
-- Идемпотентно. На существующей БД db-init пропускает (как 014), пока
-- FORCE_NOMENCLATURE_REFRESH=1.
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.LaborIntensities', N'U') IS NULL
BEGIN
    PRINT '025 skipped: LaborIntensities missing';
END
ELSE
BEGIN
BEGIN TRANSACTION;

DECLARE @PrefixMap TABLE
(
    Prefix nvarchar(10) NOT NULL PRIMARY KEY,
    IicCode nvarchar(20) NULL
);

INSERT INTO @PrefixMap (Prefix, IicCode) VALUES
    (N'ОС', NULL),
    (N'AC', N'IIC-3'),
    (N'AR', N'IIC-2'),
    (N'AS', N'IIC-3'),
    (N'CE', NULL),
    (N'CS', N'IIC-1'),
    (N'EC', N'IIC-3'),
    (N'EE', N'IIC-6'),
    (N'EL', N'IIC-3'),
    (N'EQ', NULL),
    (N'ES', N'IIC-6'),
    (N'FB', N'IIC-3'),
    (N'FD', N'IIC-1'),
    (N'FP', N'IIC-3'),
    (N'GL', NULL),
    (N'HS', N'IIC-4'),
    (N'IBS', NULL),
    (N'IE', N'IIC-4'),
    (N'IP', N'IIC-4'),
    (N'KY', NULL),
    (N'ML', N'IIC-6'),
    (N'OC', NULL),
    (N'PA', NULL),
    (N'PR', N'IIC-6'),
    (N'PS', NULL),
    (N'RA', NULL),
    (N'RR', N'IIC-2'),
    (N'RS', NULL),
    (N'SA', NULL),
    (N'SD', NULL),
    (N'SP', NULL),
    (N'SR', NULL),
    (N'SS', N'IIC-2'),
    (N'SW', NULL),
    (N'TE', N'IIC-4'),
    (N'TS', NULL),
    (N'UH', N'IIC-4'),
    (N'UL', N'IIC-4'),
    (N'UW', N'IIC-4'),
    (N'VS', N'IIC-4'),
    (N'WS', NULL);

-- Листья с явным кодом IIC.
UPDATE li
SET li.IicWorkDirectionId = d.Id,
    li.ModifiedAt = SYSUTCDATETIME()
FROM dbo.LaborIntensities AS li
CROSS APPLY (
    SELECT TOP (1) m.IicCode
    FROM @PrefixMap AS m
    WHERE li.Name LIKE m.Prefix + N'[_]%'
    ORDER BY LEN(m.Prefix) DESC
) AS mapped
INNER JOIN dbo.IicWorkDirections AS d ON d.Code = mapped.IicCode
WHERE mapped.IicCode IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.LaborIntensities AS c WHERE c.ParentId = li.Id
  )
  AND (li.IicWorkDirectionId IS NULL OR li.IicWorkDirectionId <> d.Id);

DECLARE @SetIic int = @@ROWCOUNT;

-- Листья без направления в Excel.
UPDATE li
SET li.IicWorkDirectionId = NULL,
    li.ModifiedAt = SYSUTCDATETIME()
FROM dbo.LaborIntensities AS li
CROSS APPLY (
    SELECT TOP (1) m.IicCode
    FROM @PrefixMap AS m
    WHERE li.Name LIKE m.Prefix + N'[_]%'
    ORDER BY LEN(m.Prefix) DESC
) AS mapped
WHERE mapped.IicCode IS NULL
  AND li.IicWorkDirectionId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dbo.LaborIntensities AS c WHERE c.ParentId = li.Id
  );

DECLARE @Cleared int = @@ROWCOUNT;

-- L2: все дети-листья с одним и тем же IicWorkDirectionId (в т.ч. все NULL).
;WITH Leaf AS (
    SELECT li.ParentId, li.IicWorkDirectionId
    FROM dbo.LaborIntensities AS li
    WHERE li.ParentId IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM dbo.LaborIntensities AS c WHERE c.ParentId = li.Id
      )
),
Consensus AS (
    SELECT
        ParentId,
        MIN(IicWorkDirectionId) AS IicWorkDirectionId
    FROM Leaf
    GROUP BY ParentId
    HAVING COUNT(DISTINCT ISNULL(IicWorkDirectionId, -1)) = 1
)
UPDATE p
SET p.IicWorkDirectionId = c.IicWorkDirectionId,
    p.ModifiedAt = SYSUTCDATETIME()
FROM dbo.LaborIntensities AS p
INNER JOIN Consensus AS c ON c.ParentId = p.Id
WHERE ISNULL(p.IicWorkDirectionId, -1) <> ISNULL(c.IicWorkDirectionId, -1);

DECLARE @Parents int = @@ROWCOUNT;

COMMIT TRANSACTION;

PRINT CONCAT(N'025 LaborIntensity IIC refresh: leaves set=', @SetIic,
             N', cleared=', @Cleared, N', L2 parents=', @Parents);
END;
