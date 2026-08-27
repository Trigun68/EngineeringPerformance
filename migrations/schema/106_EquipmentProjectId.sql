-- 106: Equipments.ProjectId — проектный справочник оборудования
-- Backfill: клонирование по проектам-потребителям из ProjectEquipments.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Equipments') AND name = N'ProjectId')
BEGIN
    ALTER TABLE dbo.Equipments
        ADD ProjectId INT NULL;
    PRINT 'Added Equipments.ProjectId';
END
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Equipments_Projects_ProjectId'
      AND parent_object_id = OBJECT_ID(N'dbo.Equipments')
)
BEGIN
    ALTER TABLE dbo.Equipments WITH CHECK
        ADD CONSTRAINT FK_Equipments_Projects_ProjectId
            FOREIGN KEY (ProjectId) REFERENCES dbo.Projects (Id);
    PRINT 'Added FK_Equipments_Projects_ProjectId';
END
GO

-- Иерархия между проектами не переносится: flatten ParentId перед клонированием.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

UPDATE dbo.Equipments
SET ParentId = NULL
WHERE ParentId IS NOT NULL
  AND ProjectId IS NULL;

PRINT 'Flattened Equipments.ParentId for unscoped rows';
GO

-- Backfill ProjectId / clone per consumer project (идемпотентно: только ProjectId IS NULL).
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'tempdb..#EqConsumers', N'U') IS NOT NULL DROP TABLE #EqConsumers;
IF OBJECT_ID(N'tempdb..#EqCloneMap', N'U') IS NOT NULL DROP TABLE #EqCloneMap;

;WITH Consumers AS (
    SELECT DISTINCT pe.EquipmentId, pe.ProjectId
    FROM dbo.ProjectEquipments pe
    INNER JOIN dbo.Equipments e ON e.Id = pe.EquipmentId
    WHERE e.ProjectId IS NULL
      AND pe.ProjectId IS NOT NULL
)
SELECT EquipmentId, ProjectId
INTO #EqConsumers
FROM Consumers;

-- 0 потребителей: orphan → архив, ProjectId остаётся NULL
UPDATE e
SET e.IsArchived = 1,
    e.ModifiedAt = SYSUTCDATETIME()
FROM dbo.Equipments e
WHERE e.ProjectId IS NULL
  AND NOT EXISTS (SELECT 1 FROM #EqConsumers c WHERE c.EquipmentId = e.Id)
  AND e.IsArchived = 0;

-- 1 потребитель: присвоить ProjectId
;WITH Single AS (
    SELECT EquipmentId, MIN(ProjectId) AS ProjectId
    FROM #EqConsumers
    GROUP BY EquipmentId
    HAVING COUNT(*) = 1
)
UPDATE e
SET e.ProjectId = s.ProjectId,
    e.ModifiedAt = SYSUTCDATETIME()
FROM dbo.Equipments e
INNER JOIN Single s ON s.EquipmentId = e.Id
WHERE e.ProjectId IS NULL;

-- N потребителей: оригинал → min(ProjectId); клоны для остальных
CREATE TABLE #EqCloneMap (
    SourceEquipmentId INT NOT NULL,
    TargetProjectId INT NOT NULL,
    NewEquipmentId INT NULL,
    PRIMARY KEY (SourceEquipmentId, TargetProjectId)
);

;WITH Multi AS (
    SELECT EquipmentId
    FROM #EqConsumers
    GROUP BY EquipmentId
    HAVING COUNT(*) > 1
),
Owner AS (
    SELECT c.EquipmentId, MIN(c.ProjectId) AS OwnerProjectId
    FROM #EqConsumers c
    INNER JOIN Multi m ON m.EquipmentId = c.EquipmentId
    GROUP BY c.EquipmentId
)
UPDATE e
SET e.ProjectId = o.OwnerProjectId,
    e.ModifiedAt = SYSUTCDATETIME()
FROM dbo.Equipments e
INNER JOIN Owner o ON o.EquipmentId = e.Id
WHERE e.ProjectId IS NULL;

INSERT INTO #EqCloneMap (SourceEquipmentId, TargetProjectId)
SELECT c.EquipmentId, c.ProjectId
FROM #EqConsumers c
INNER JOIN (
    SELECT EquipmentId, MIN(ProjectId) AS OwnerProjectId
    FROM #EqConsumers
    GROUP BY EquipmentId
    HAVING COUNT(*) > 1
) o ON o.EquipmentId = c.EquipmentId
WHERE c.ProjectId <> o.OwnerProjectId;

DECLARE @SourceId INT, @TargetProjectId INT, @NewId INT;

DECLARE clone_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT SourceEquipmentId, TargetProjectId
    FROM #EqCloneMap
    WHERE NewEquipmentId IS NULL
    ORDER BY SourceEquipmentId, TargetProjectId;

OPEN clone_cursor;
FETCH NEXT FROM clone_cursor INTO @SourceId, @TargetProjectId;

WHILE @@FETCH_STATUS = 0
BEGIN
    INSERT INTO dbo.Equipments (
        Name, Description, ClassifierId, UnitId, Coefficient, IsAssessment,
        ParentId, SortOrder, IsArchived, CreatedAt, ModifiedAt, CatalogKeyHash, ProjectId
    )
    SELECT
        e.Name,
        e.Description,
        e.ClassifierId,
        e.UnitId,
        e.Coefficient,
        e.IsAssessment,
        NULL,
        e.SortOrder,
        e.IsArchived,
        SYSUTCDATETIME(),
        SYSUTCDATETIME(),
        -- временный уникальный hash до пересчёта
        LOWER(CONVERT(varchar(64), HASHBYTES(
            'SHA2_256',
            CONCAT(N'clone:', CAST(@SourceId AS nvarchar(20)), N':', CAST(@TargetProjectId AS nvarchar(20)), N':', e.Name)
        ), 2)),
        @TargetProjectId
    FROM dbo.Equipments e
    WHERE e.Id = @SourceId;

    SET @NewId = SCOPE_IDENTITY();

    UPDATE #EqCloneMap
    SET NewEquipmentId = @NewId
    WHERE SourceEquipmentId = @SourceId
      AND TargetProjectId = @TargetProjectId;

    UPDATE pe
    SET pe.EquipmentId = @NewId
    FROM dbo.ProjectEquipments pe
    WHERE pe.EquipmentId = @SourceId
      AND pe.ProjectId = @TargetProjectId;

    FETCH NEXT FROM clone_cursor INTO @SourceId, @TargetProjectId;
END

CLOSE clone_cursor;
DEALLOCATE clone_cursor;

DECLARE @CloneCount INT = (SELECT COUNT(*) FROM #EqCloneMap WHERE NewEquipmentId IS NOT NULL);
PRINT CONCAT('Equipment clones created: ', @CloneCount);

DROP TABLE #EqCloneMap;
DROP TABLE #EqConsumers;
GO

-- Пересчёт CatalogKeyHash:
-- ProjectId IS NULL → legacy (Name + UnitId) для совместимости с 014 nomenclature
-- ProjectId NOT NULL → ProjectId + Name + UnitId
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

UPDATE dbo.Equipments
SET CatalogKeyHash = LOWER(CONVERT(varchar(64), HASHBYTES(
        'SHA2_256',
        CONCAT(
            CASE WHEN ProjectId IS NULL THEN N'' ELSE CONCAT(CAST(ProjectId AS nvarchar(20)), CHAR(0)) END,
            LTRIM(RTRIM(Name)),
            CHAR(0),
            COALESCE(CAST(UnitId AS nvarchar(20)), N'')
        )
    ), 2)),
    ModifiedAt = SYSUTCDATETIME();

PRINT 'Recalculated Equipments.CatalogKeyHash';
GO

-- Индексы
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Equipments_CatalogKeyHash'
      AND object_id = OBJECT_ID(N'dbo.Equipments')
)
BEGIN
    DROP INDEX IX_Equipments_CatalogKeyHash ON dbo.Equipments;
    PRINT 'Dropped IX_Equipments_CatalogKeyHash';
END
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Equipments_ProjectId_CatalogKeyHash'
      AND object_id = OBJECT_ID(N'dbo.Equipments')
)
BEGIN
    CREATE UNIQUE INDEX IX_Equipments_ProjectId_CatalogKeyHash
        ON dbo.Equipments (ProjectId, CatalogKeyHash)
        WHERE ProjectId IS NOT NULL;
    PRINT 'Created IX_Equipments_ProjectId_CatalogKeyHash';
END
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Equipments_NullProject_CatalogKeyHash'
      AND object_id = OBJECT_ID(N'dbo.Equipments')
)
BEGIN
    CREATE UNIQUE INDEX IX_Equipments_NullProject_CatalogKeyHash
        ON dbo.Equipments (CatalogKeyHash)
        WHERE ProjectId IS NULL;
    PRINT 'Created IX_Equipments_NullProject_CatalogKeyHash';
END
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_Equipments_ProjectId_IsArchived_ParentId_SortOrder'
      AND object_id = OBJECT_ID(N'dbo.Equipments')
)
BEGIN
    CREATE INDEX IX_Equipments_ProjectId_IsArchived_ParentId_SortOrder
        ON dbo.Equipments (ProjectId, IsArchived, ParentId, SortOrder);
    PRINT 'Created IX_Equipments_ProjectId_IsArchived_ParentId_SortOrder';
END
GO

PRINT '106_EquipmentProjectId completed';
GO
