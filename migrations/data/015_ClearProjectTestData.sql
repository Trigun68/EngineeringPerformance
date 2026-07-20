-- ============================================================================
-- 015: Удаление всех данных проектов (карточки, версии, ФО, ТЕ, импорты).
-- Справочники (ProjectCharacteristics, KFO/BER/IIC и т.д.) не затрагиваются.
-- Идемпотентно: повторный запуск на пустой БД безопасен.
-- ============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectLaborIntensities')
    DELETE FROM [ProjectLaborIntensities];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectPhysicalVolumes')
    DELETE FROM [ProjectPhysicalVolumes];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectEquipments')
    DELETE FROM [ProjectEquipments];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'DataImportIterations')
    DELETE FROM [DataImportIterations];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AnalysisVersionSnapshots')
    DELETE FROM [AnalysisVersionSnapshots];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectVersionPlanningParameters')
    DELETE FROM [ProjectVersionPlanningParameters];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectCharacteristicSelections')
    DELETE FROM [ProjectCharacteristicSelections];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectPermissions')
    DELETE FROM [ProjectPermissions];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectUserAccess')
    DELETE FROM [ProjectUserAccess];

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectAnalysisVersions')
BEGIN
    UPDATE [ProjectAnalysisVersions] SET [ParentId] = NULL WHERE [ParentId] IS NOT NULL;
    DELETE FROM [ProjectAnalysisVersions];
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Projects')
    DELETE FROM [Projects];

-- Сброс identity для чистого старта новых проектов
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'Projects')
    DBCC CHECKIDENT (N'Projects', RESEED, 0);

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ProjectAnalysisVersions')
    DBCC CHECKIDENT (N'ProjectAnalysisVersions', RESEED, 0);

COMMIT;
GO
