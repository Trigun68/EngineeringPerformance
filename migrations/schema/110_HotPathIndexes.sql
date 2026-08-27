-- 110: Hot-path indexes for version-scoped PPV / ProjectEquipments (SqlQueryMetrics 48h)
-- Baseline 2026-07-27: PPV+KFO+BER TotalMs ~55s (1012 exec, max ~999ms);
--                       PE+Equipments TotalMs ~10s (966 exec, max ~252ms).
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

-- 1) ProjectEquipments: (VersionId, EquipmentId) for /performance and import replace
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectEquipments_VersionId_EquipmentId'
      AND object_id = OBJECT_ID(N'dbo.ProjectEquipments'))
BEGIN
    CREATE INDEX IX_ProjectEquipments_VersionId_EquipmentId
        ON dbo.ProjectEquipments (ProjectAnalysisVersionId, EquipmentId);
    PRINT 'Created IX_ProjectEquipments_VersionId_EquipmentId';
END
ELSE
    PRINT 'IX_ProjectEquipments_VersionId_EquipmentId already exists';

-- 2) ProjectPhysicalVolumes: covering seek by version+project (typical EF grid load)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering'
      AND object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes'))
BEGIN
    CREATE INDEX IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering
        ON dbo.ProjectPhysicalVolumes (ProjectAnalysisVersionId, ProjectId)
        INCLUDE (
            PhysicalVolumeKfoId,
            PhysicalVolumeBerId,
            Quantity,
            LaborIntensityBase,
            LaborIntensityOptimistic,
            LaborIntensityPessimistic,
            FacilityCode,
            FacilityName,
            DocumentSet,
            CatalogCode,
            CatalogId,
            WorkId,
            ParentId,
            BindingType
        );
    PRINT 'Created IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering';
END
ELSE
    PRINT 'IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering already exists';
