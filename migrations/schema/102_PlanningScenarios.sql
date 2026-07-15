-- 102: Сценарии планирования + столбцы трудоёмкости на строках ФО
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProjectAnalysisVersions') AND name = N'PlanningScenario')
BEGIN
    ALTER TABLE dbo.ProjectAnalysisVersions ADD PlanningScenario INT NOT NULL CONSTRAINT DF_PAV_PlanningScenario DEFAULT 1;
    PRINT 'Added PlanningScenario';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes') AND name = N'LaborIntensityBase')
BEGIN
    ALTER TABLE dbo.ProjectPhysicalVolumes ADD LaborIntensityBase DECIMAL(18,4) NULL;
    PRINT 'Added LaborIntensityBase';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes') AND name = N'LaborIntensityPessimistic')
BEGIN
    ALTER TABLE dbo.ProjectPhysicalVolumes ADD LaborIntensityPessimistic DECIMAL(18,4) NULL;
    PRINT 'Added LaborIntensityPessimistic';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes') AND name = N'LaborIntensityOptimistic')
BEGIN
    ALTER TABLE dbo.ProjectPhysicalVolumes ADD LaborIntensityOptimistic DECIMAL(18,4) NULL;
    PRINT 'Added LaborIntensityOptimistic';
END

COMMIT TRANSACTION;
PRINT '102_PlanningScenarios completed';
