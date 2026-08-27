-- 111: Wave-2 indexes from SqlQueryMetrics (168h TotalMs / 48h P95)
-- Targets: LI version load (P95 ~142ms), norms as-of covering, import iteration list.
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- 1) ProjectLaborIntensities: covering seek by version (page-data / performance raw loader)
-- SELECT columns from hash f1309808… (full entity + join to IIC; cover LI side only).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectLaborIntensities_VersionId_Covering'
      AND object_id = OBJECT_ID(N'dbo.ProjectLaborIntensities'))
BEGIN
    CREATE INDEX IX_ProjectLaborIntensities_VersionId_Covering
        ON dbo.ProjectLaborIntensities (ProjectAnalysisVersionId)
        INCLUDE (
            ProjectId,
            DocumentationCode,
            WorkDirectionId,
            PlannedHours,
            ActualHours,
            Notes,
            Brand,
            CreatedAt,
            ModifiedAt,
            PhysicalVolumeKfoId,
            PhysicalVolumeBerId,
            LaborIntensityId,
            ProjectPhysicalVolumeId
        );
    PRINT 'Created IX_ProjectLaborIntensities_VersionId_Covering';
END
ELSE
    PRINT 'IX_ProjectLaborIntensities_VersionId_Covering already exists';

-- 2) IicWorkDirectionNorms: covering unique (WorkDirectionId, EffectiveFrom)
-- Already had UX without INCLUDE; recreate as covering for as-of lookups.
IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom'
      AND object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms'))
BEGIN
    DECLARE @hasInclude bit = 0;
    IF EXISTS (
        SELECT 1
        FROM sys.index_columns ic
        JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        WHERE i.name = N'UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom'
          AND i.object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms')
          AND ic.is_included_column = 1)
        SET @hasInclude = 1;

    IF @hasInclude = 0
    BEGIN
        DROP INDEX UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom
            ON dbo.IicWorkDirectionNorms;

        CREATE UNIQUE INDEX UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom
            ON dbo.IicWorkDirectionNorms (WorkDirectionId, EffectiveFrom DESC)
            INCLUDE (
                EffectiveTo,
                UnitId,
                Performance,
                Estimate,
                StandardDeviation,
                FormulaType,
                CoefficientsJson,
                MinVolume,
                MaxVolume,
                ExtrapolationMode,
                DeviationModelType,
                DeviationCoefficientsJson
            );
        PRINT 'Recreated UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom with INCLUDE';
    END
    ELSE
        PRINT 'UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom already covering';
END
ELSE
BEGIN
    CREATE UNIQUE INDEX UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom
        ON dbo.IicWorkDirectionNorms (WorkDirectionId, EffectiveFrom DESC)
        INCLUDE (
            EffectiveTo,
            UnitId,
            Performance,
            Estimate,
            StandardDeviation,
            FormulaType,
            CoefficientsJson,
            MinVolume,
            MaxVolume,
            ExtrapolationMode,
            DeviationModelType,
            DeviationCoefficientsJson
        );
    PRINT 'Created UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom (covering)';
END

-- 3) DataImportIterations: list by project/version/type ordered by ImportedAt
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_DataImportIterations_Project_Version_DataType_ImportedAt'
      AND object_id = OBJECT_ID(N'dbo.DataImportIterations'))
BEGIN
    CREATE INDEX IX_DataImportIterations_Project_Version_DataType_ImportedAt
        ON dbo.DataImportIterations (ProjectId, ProjectAnalysisVersionId, DataType, ImportedAt DESC);
    PRINT 'Created IX_DataImportIterations_Project_Version_DataType_ImportedAt';
END
ELSE
    PRINT 'IX_DataImportIterations_Project_Version_DataType_ImportedAt already exists';
