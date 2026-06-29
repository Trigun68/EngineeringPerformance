-- ============================================================================
-- Consolidated Migration: 2026-04-15
-- Combines 4 EF Core migrations into a single idempotent SQL script
-- Baseline: 20260414214934_AddProjectPhysicalVolumeDetailsAndLaborIntensityBrand
-- ============================================================================
-- Migrations included:
--   1. 20260415181235_AddIicNormPerformanceFields
--   2. 20260415183559_AddAnalysisDirectionAndVolumeTypeToProjectAnalysisVersion
--   3. 20260415193900_AddPhysicalVolumeRelationships
--   4. 20260415202426_AddDataImportIterations
-- ============================================================================

-- ============================================================================
-- Migration 1: AddIicNormPerformanceFields
-- Adds Performance, Estimate, StandardDeviation to IicWorkDirectionNorms
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[IicWorkDirectionNorms]')
    AND name = N'Performance'
)
BEGIN
    ALTER TABLE [IicWorkDirectionNorms]
    ADD [Performance] decimal(18,4) NULL;
    PRINT 'Added column Performance to IicWorkDirectionNorms';
END
ELSE
BEGIN
    PRINT 'Column Performance already exists in IicWorkDirectionNorms';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[IicWorkDirectionNorms]')
    AND name = N'Estimate'
)
BEGIN
    ALTER TABLE [IicWorkDirectionNorms]
    ADD [Estimate] decimal(18,4) NULL;
    PRINT 'Added column Estimate to IicWorkDirectionNorms';
END
ELSE
BEGIN
    PRINT 'Column Estimate already exists in IicWorkDirectionNorms';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[IicWorkDirectionNorms]')
    AND name = N'StandardDeviation'
)
BEGIN
    ALTER TABLE [IicWorkDirectionNorms]
    ADD [StandardDeviation] decimal(18,4) NULL;
    PRINT 'Added column StandardDeviation to IicWorkDirectionNorms';
END
ELSE
BEGIN
    PRINT 'Column StandardDeviation already exists in IicWorkDirectionNorms';
END
GO

-- ============================================================================
-- Migration 2: AddAnalysisDirectionAndVolumeTypeToProjectAnalysisVersion
-- Adds Direction (int, default 1) and PhysicalVolumeTypeId (FK) to ProjectAnalysisVersions
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ProjectAnalysisVersions]')
    AND name = N'Direction'
)
BEGIN
    ALTER TABLE [ProjectAnalysisVersions]
    ADD [Direction] int NOT NULL DEFAULT 1;
    PRINT 'Added column Direction to ProjectAnalysisVersions (default: 1 = Plan)';
END
ELSE
BEGIN
    PRINT 'Column Direction already exists in ProjectAnalysisVersions';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ProjectAnalysisVersions]')
    AND name = N'PhysicalVolumeTypeId'
)
BEGIN
    ALTER TABLE [ProjectAnalysisVersions]
    ADD [PhysicalVolumeTypeId] int NULL;
    PRINT 'Added column PhysicalVolumeTypeId to ProjectAnalysisVersions';
END
ELSE
BEGIN
    PRINT 'Column PhysicalVolumeTypeId already exists in ProjectAnalysisVersions';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectAnalysisVersions_PhysicalVolumeTypeId'
    AND object_id = OBJECT_ID(N'[ProjectAnalysisVersions]')
)
BEGIN
    CREATE INDEX [IX_ProjectAnalysisVersions_PhysicalVolumeTypeId]
    ON [ProjectAnalysisVersions] ([PhysicalVolumeTypeId]);
    PRINT 'Created index IX_ProjectAnalysisVersions_PhysicalVolumeTypeId';
END
ELSE
BEGIN
    PRINT 'Index IX_ProjectAnalysisVersions_PhysicalVolumeTypeId already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ProjectAnalysisVersions_PhysicalVolumeTypes_PhysicalVolumeTypeId'
)
BEGIN
    ALTER TABLE [ProjectAnalysisVersions]
    ADD CONSTRAINT [FK_ProjectAnalysisVersions_PhysicalVolumeTypes_PhysicalVolumeTypeId]
    FOREIGN KEY ([PhysicalVolumeTypeId])
    REFERENCES [PhysicalVolumeTypes] ([Id])
    ON DELETE SET NULL;
    PRINT 'Created FK: FK_ProjectAnalysisVersions_PhysicalVolumeTypes_PhysicalVolumeTypeId';
END
ELSE
BEGIN
    PRINT 'FK FK_ProjectAnalysisVersions_PhysicalVolumeTypes_PhysicalVolumeTypeId already exists';
END
GO

-- ============================================================================
-- Migration 3: AddPhysicalVolumeRelationships
-- Adds PhysicalVolumeId FK to ProjectLaborIntensities, DocumentSet to PhysicalVolumes,
-- and various indexes for business-level matching
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ProjectLaborIntensities]')
    AND name = N'PhysicalVolumeId'
)
BEGIN
    ALTER TABLE [ProjectLaborIntensities]
    ADD [PhysicalVolumeId] int NULL;
    PRINT 'Added column PhysicalVolumeId to ProjectLaborIntensities';
END
ELSE
BEGIN
    PRINT 'Column PhysicalVolumeId already exists in ProjectLaborIntensities';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[PhysicalVolumes]')
    AND name = N'DocumentSet'
)
BEGIN
    ALTER TABLE [PhysicalVolumes]
    ADD [DocumentSet] nvarchar(100) NULL;
    PRINT 'Added column DocumentSet to PhysicalVolumes';
END
ELSE
BEGIN
    PRINT 'Column DocumentSet already exists in PhysicalVolumes';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectPhysicalVolumes_CatalogId'
    AND object_id = OBJECT_ID(N'[ProjectPhysicalVolumes]')
)
BEGIN
    CREATE INDEX [IX_ProjectPhysicalVolumes_CatalogId]
    ON [ProjectPhysicalVolumes] ([CatalogId]);
    PRINT 'Created index IX_ProjectPhysicalVolumes_CatalogId';
END
ELSE
BEGIN
    PRINT 'Index IX_ProjectPhysicalVolumes_CatalogId already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectLaborIntensities_Brand'
    AND object_id = OBJECT_ID(N'[ProjectLaborIntensities]')
)
BEGIN
    CREATE INDEX [IX_ProjectLaborIntensities_Brand]
    ON [ProjectLaborIntensities] ([Brand]);
    PRINT 'Created index IX_ProjectLaborIntensities_Brand';
END
ELSE
BEGIN
    PRINT 'Index IX_ProjectLaborIntensities_Brand already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectLaborIntensities_PhysicalVolumeId'
    AND object_id = OBJECT_ID(N'[ProjectLaborIntensities]')
)
BEGIN
    CREATE INDEX [IX_ProjectLaborIntensities_PhysicalVolumeId]
    ON [ProjectLaborIntensities] ([PhysicalVolumeId]);
    PRINT 'Created index IX_ProjectLaborIntensities_PhysicalVolumeId';
END
ELSE
BEGIN
    PRINT 'Index IX_ProjectLaborIntensities_PhysicalVolumeId already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PhysicalVolumes_DocumentSet'
    AND object_id = OBJECT_ID(N'[PhysicalVolumes]')
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_DocumentSet]
    ON [PhysicalVolumes] ([DocumentSet]);
    PRINT 'Created index IX_PhysicalVolumes_DocumentSet';
END
ELSE
BEGIN
    PRINT 'Index IX_PhysicalVolumes_DocumentSet already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId'
)
BEGIN
    ALTER TABLE [ProjectLaborIntensities]
    ADD CONSTRAINT [FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId]
    FOREIGN KEY ([PhysicalVolumeId])
    REFERENCES [PhysicalVolumes] ([Id]);
    PRINT 'Created FK: FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId';
END
ELSE
BEGIN
    PRINT 'FK FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId already exists';
END
GO

-- ============================================================================
-- Migration 4: AddDataImportIterations
-- Creates DataImportIterations table for tracking Excel import iterations
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'DataImportIterations'
    AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [DataImportIterations] (
        [Id] int NOT NULL IDENTITY(1,1),
        [ProjectId] int NOT NULL,
        [ProjectAnalysisVersionId] int NOT NULL,
        [DataType] int NOT NULL,
        [ImportedAt] datetime2 NOT NULL,
        [Description] nvarchar(500) NULL,
        [RecordCount] int NOT NULL,
        CONSTRAINT [PK_DataImportIterations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DataImportIterations_ProjectAnalysisVersions_ProjectAnalysisVersionId]
            FOREIGN KEY ([ProjectAnalysisVersionId])
            REFERENCES [ProjectAnalysisVersions] ([Id])
            ON DELETE CASCADE,
        CONSTRAINT [FK_DataImportIterations_Projects_ProjectId]
            FOREIGN KEY ([ProjectId])
            REFERENCES [Projects] ([Id])
            ON DELETE NO ACTION
    );
    PRINT 'Created table DataImportIterations';
END
ELSE
BEGIN
    PRINT 'Table DataImportIterations already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DataImportIterations_ProjectAnalysisVersionId'
    AND object_id = OBJECT_ID(N'[DataImportIterations]')
)
BEGIN
    CREATE INDEX [IX_DataImportIterations_ProjectAnalysisVersionId]
    ON [DataImportIterations] ([ProjectAnalysisVersionId]);
    PRINT 'Created index IX_DataImportIterations_ProjectAnalysisVersionId';
END
ELSE
BEGIN
    PRINT 'Index IX_DataImportIterations_ProjectAnalysisVersionId already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DataImportIterations_ProjectAnalysisVersionId_DataType'
    AND object_id = OBJECT_ID(N'[DataImportIterations]')
)
BEGIN
    CREATE INDEX [IX_DataImportIterations_ProjectAnalysisVersionId_DataType]
    ON [DataImportIterations] ([ProjectAnalysisVersionId], [DataType]);
    PRINT 'Created index IX_DataImportIterations_ProjectAnalysisVersionId_DataType';
END
ELSE
BEGIN
    PRINT 'Index IX_DataImportIterations_ProjectAnalysisVersionId_DataType already exists';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DataImportIterations_ProjectId'
    AND object_id = OBJECT_ID(N'[DataImportIterations]')
)
BEGIN
    CREATE INDEX [IX_DataImportIterations_ProjectId]
    ON [DataImportIterations] ([ProjectId]);
    PRINT 'Created index IX_DataImportIterations_ProjectId';
END
ELSE
BEGIN
    PRINT 'Index IX_DataImportIterations_ProjectId already exists';
END
GO

-- ============================================================================
-- Record migration in __EFMigrationsHistory
-- ============================================================================

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415181235_AddIicNormPerformanceFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415181235_AddIicNormPerformanceFields', N'10.0.5');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415183559_AddAnalysisDirectionAndVolumeTypeToProjectAnalysisVersion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415183559_AddAnalysisDirectionAndVolumeTypeToProjectAnalysisVersion', N'10.0.5');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415193900_AddPhysicalVolumeRelationships'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415193900_AddPhysicalVolumeRelationships', N'10.0.5');
END
GO

IF NOT EXISTS (
    SELECT 1 FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260415202426_AddDataImportIterations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260415202426_AddDataImportIterations', N'10.0.5');
END
GO

PRINT 'Consolidated migration 2026-04-15 completed successfully.'
GO
