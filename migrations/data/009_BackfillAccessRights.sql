-- ============================================================================
-- 009: Idempotent backfill AccessRights for all permission objects
-- Добавляет права для новых объектов (SettingsPage, UserActionLog, Dashboard и др.),
-- если комбинация (RoleId, ObjectName) ещё отсутствует в БД.
-- ============================================================================
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @PmRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Admin');
DECLARE @ObsRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Engineer');

IF @PmRoleId IS NULL OR @ObsRoleId IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    RETURN;
END;

DECLARE @Objects TABLE ([Name] NVARCHAR(100));
INSERT INTO @Objects ([Name])
VALUES
    (N'Dashboard'),
    (N'Users'),
    (N'Projects'),
    (N'PhysicalVolumes'),
    (N'PhysicalVolumeTypes'),
    (N'Equipment'),
    (N'IicWorkDirections'),
    (N'ProjectAnalysisVersions'),
    (N'IicWorkDirectionNorms'),
    (N'UnitsOfMeasurement'),
    (N'ProjectPhysicalVolumes'),
    (N'ProjectEquipments'),
    (N'ProjectLaborIntensities'),
    (N'ProjectDataUpload'),
    (N'Assessment'),
    (N'Planning'),
    (N'VersionComparison'),
    (N'GroupAnalysis'),
    (N'GroupTrend'),
    (N'References'),
    (N'DataPage'),
    (N'SettingsPage'),
    (N'HelpPage'),
    (N'LaborIntensities'),
    (N'ProjectCharacteristics'),
    (N'UserActionLog');

INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
SELECT @PmRoleId, o.[Name], 2
FROM @Objects o
WHERE NOT EXISTS (
    SELECT 1 FROM [AccessRights] ar
    WHERE ar.[RoleId] = @PmRoleId AND ar.[ObjectName] = o.[Name]
);

INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
SELECT @ObsRoleId, o.[Name], 1
FROM @Objects o
WHERE NOT EXISTS (
    SELECT 1 FROM [AccessRights] ar
    WHERE ar.[RoleId] = @ObsRoleId AND ar.[ObjectName] = o.[Name]
);

COMMIT;
GO
