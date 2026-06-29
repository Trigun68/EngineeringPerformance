-- ============================================================================
-- Consolidated Migration: 2026-04-13 .. 2026-04-14
-- Объединяет 6 миграций EF Core в один идемпотентный SQL-скрипт.
-- Выполняется ПОСЛЕ 001_InitialSchema.sql и ДО 006_Consolidated_20260415.sql.
-- ============================================================================
-- В состав входят:
--   1. 20260413064910_UpdateModel
--      — PhysicalVolumes.Description: nvarchar(1000) -> nvarchar(4000)
--   2. 20260413143229_AddCodeToPhysicalVolumeType
--      — PhysicalVolumeTypes.Code nvarchar(50) NOT NULL DEFAULT ''
--   3. 20260413154641_AddSortOrderToDirectoryItems
--      — SortOrder int NOT NULL DEFAULT 0 в 8 таблицах-справочниках
--   4. 20260413155534_AddIndexesForPerformance
--      — IX_PhysicalVolumes_IsArchived, IX_IicWorkDirections_IsArchived
--   5. 20260414211006_AddIsAssessmentToPhysicalVolume
--      — PhysicalVolumes.IsAssessment bit NULL + IX_PhysicalVolumes_IsAssessment
--   6. 20260414214934_AddProjectPhysicalVolumeDetailsAndLaborIntensityBrand
--      — дополнительные поля в ProjectPhysicalVolumes и ProjectLaborIntensities
-- ============================================================================

-- ============================================================================
-- Migration 1: UpdateModel — расширение PhysicalVolumes.Description до 4000
-- ============================================================================
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[PhysicalVolumes]')
      AND name = N'Description'
      AND max_length <> 8000  -- 4000 * 2 (nvarchar)
)
BEGIN
    ALTER TABLE [PhysicalVolumes]
    ALTER COLUMN [Description] nvarchar(4000) NOT NULL;
    PRINT 'Altered column PhysicalVolumes.Description to nvarchar(4000)';
END
ELSE
BEGIN
    PRINT 'Column PhysicalVolumes.Description already nvarchar(4000) (or missing)';
END
GO

-- ============================================================================
-- Migration 2: AddCodeToPhysicalVolumeType
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[PhysicalVolumeTypes]')
      AND name = N'Code'
)
BEGIN
    ALTER TABLE [PhysicalVolumeTypes]
    ADD [Code] nvarchar(50) NOT NULL
        CONSTRAINT [DF_PhysicalVolumeTypes_Code] DEFAULT (N'');
    PRINT 'Added column PhysicalVolumeTypes.Code';
END
ELSE
BEGIN
    PRINT 'Column PhysicalVolumeTypes.Code already exists';
END
GO

-- ============================================================================
-- Migration 3: AddSortOrderToDirectoryItems
-- Добавляет SortOrder int NOT NULL DEFAULT 0 в 8 таблиц-справочников.
-- ============================================================================
DECLARE @SortOrderTargets TABLE ([TableName] sysname);
INSERT INTO @SortOrderTargets ([TableName])
VALUES
    (N'Users'),
    (N'UnitsOfMeasurement'),
    (N'Roles'),
    (N'Projects'),
    (N'ProjectAnalysisVersions'),
    (N'PhysicalVolumeTypes'),
    (N'PhysicalVolumes'),
    (N'IicWorkDirections');

DECLARE @Tbl sysname;
DECLARE @Sql nvarchar(max);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT [TableName] FROM @SortOrderTargets;

OPEN cur;
FETCH NEXT FROM cur INTO @Tbl;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(QUOTENAME(@Tbl))
          AND name = N'SortOrder'
    )
    BEGIN
        SET @Sql = N'ALTER TABLE ' + QUOTENAME(@Tbl) +
                   N' ADD [SortOrder] int NOT NULL CONSTRAINT ' +
                   QUOTENAME(N'DF_' + @Tbl + N'_SortOrder') +
                   N' DEFAULT (0);';
        EXEC sp_executesql @Sql;
        PRINT 'Added column SortOrder to ' + @Tbl;
    END
    ELSE
    BEGIN
        PRINT 'Column SortOrder already exists in ' + @Tbl;
    END

    FETCH NEXT FROM cur INTO @Tbl;
END

CLOSE cur;
DEALLOCATE cur;
GO

-- ============================================================================
-- Migration 4: AddIndexesForPerformance
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PhysicalVolumes_IsArchived'
      AND object_id = OBJECT_ID(N'[PhysicalVolumes]')
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_IsArchived]
        ON [PhysicalVolumes] ([IsArchived]);
    PRINT 'Created index IX_PhysicalVolumes_IsArchived';
END
ELSE
BEGIN
    PRINT 'Index IX_PhysicalVolumes_IsArchived already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_IicWorkDirections_IsArchived'
      AND object_id = OBJECT_ID(N'[IicWorkDirections]')
)
BEGIN
    CREATE INDEX [IX_IicWorkDirections_IsArchived]
        ON [IicWorkDirections] ([IsArchived]);
    PRINT 'Created index IX_IicWorkDirections_IsArchived';
END
ELSE
BEGIN
    PRINT 'Index IX_IicWorkDirections_IsArchived already exists';
END
GO

-- ============================================================================
-- Migration 5: AddIsAssessmentToPhysicalVolume
-- ============================================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[PhysicalVolumes]')
      AND name = N'IsAssessment'
)
BEGIN
    ALTER TABLE [PhysicalVolumes]
    ADD [IsAssessment] bit NULL;
    PRINT 'Added column PhysicalVolumes.IsAssessment';
END
ELSE
BEGIN
    PRINT 'Column PhysicalVolumes.IsAssessment already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PhysicalVolumes_IsAssessment'
      AND object_id = OBJECT_ID(N'[PhysicalVolumes]')
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_IsAssessment]
        ON [PhysicalVolumes] ([IsAssessment]);
    PRINT 'Created index IX_PhysicalVolumes_IsAssessment';
END
ELSE
BEGIN
    PRINT 'Index IX_PhysicalVolumes_IsAssessment already exists';
END
GO

-- ============================================================================
-- Migration 6: AddProjectPhysicalVolumeDetailsAndLaborIntensityBrand
-- Доп. поля в ProjectPhysicalVolumes (+ Brand в ProjectLaborIntensities).
-- ============================================================================
DECLARE @ProjectPvColumns TABLE ([Name] sysname, [Definition] nvarchar(200));
INSERT INTO @ProjectPvColumns ([Name], [Definition])
VALUES
    (N'BindingType',       N'nvarchar(200) NULL'),
    (N'CatalogId',         N'nvarchar(100) NULL'),
    (N'DocumentSet',       N'nvarchar(100) NULL'),
    (N'EstimationBasis',   N'nvarchar(500) NULL'),
    (N'FacilityCode',      N'nvarchar(100) NULL'),
    (N'FacilityName',      N'nvarchar(500) NULL'),
    (N'LaborHoursPerUnit', N'decimal(18,4) NULL'),
    (N'MachineHoursPerUnit', N'decimal(18,4) NULL'),
    (N'Status',            N'nvarchar(50) NULL'),
    (N'Tag',               N'nvarchar(100) NULL'),
    (N'TotalMachineHours', N'decimal(18,4) NULL'),
    (N'WorkComment',       N'nvarchar(500) NULL');

DECLARE @ColName sysname, @ColDef nvarchar(200), @AlterSql nvarchar(max);
DECLARE col_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT [Name], [Definition] FROM @ProjectPvColumns;

OPEN col_cur;
FETCH NEXT FROM col_cur INTO @ColName, @ColDef;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'[ProjectPhysicalVolumes]')
          AND name = @ColName
    )
    BEGIN
        SET @AlterSql = N'ALTER TABLE [ProjectPhysicalVolumes] ADD ' +
                        QUOTENAME(@ColName) + N' ' + @ColDef + N';';
        EXEC sp_executesql @AlterSql;
        PRINT 'Added column ProjectPhysicalVolumes.' + @ColName;
    END
    ELSE
    BEGIN
        PRINT 'Column ProjectPhysicalVolumes.' + @ColName + ' already exists';
    END

    FETCH NEXT FROM col_cur INTO @ColName, @ColDef;
END

CLOSE col_cur;
DEALLOCATE col_cur;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ProjectLaborIntensities]')
      AND name = N'Brand'
)
BEGIN
    ALTER TABLE [ProjectLaborIntensities]
    ADD [Brand] nvarchar(100) NULL;
    PRINT 'Added column ProjectLaborIntensities.Brand';
END
ELSE
BEGIN
    PRINT 'Column ProjectLaborIntensities.Brand already exists';
END
GO

-- ============================================================================
-- Регистрация миграций в __EFMigrationsHistory
-- ============================================================================
DECLARE @MigrationsToRecord TABLE ([MigrationId] nvarchar(150));
INSERT INTO @MigrationsToRecord ([MigrationId])
VALUES
    (N'20260413064910_UpdateModel'),
    (N'20260413143229_AddCodeToPhysicalVolumeType'),
    (N'20260413154641_AddSortOrderToDirectoryItems'),
    (N'20260413155534_AddIndexesForPerformance'),
    (N'20260414211006_AddIsAssessmentToPhysicalVolume'),
    (N'20260414214934_AddProjectPhysicalVolumeDetailsAndLaborIntensityBrand');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
SELECT m.[MigrationId], N'10.0.5'
FROM @MigrationsToRecord m
WHERE NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory] h
    WHERE h.[MigrationId] = m.[MigrationId]
);
GO

PRINT 'Consolidated migration 2026-04-13..14 completed successfully.'
GO
