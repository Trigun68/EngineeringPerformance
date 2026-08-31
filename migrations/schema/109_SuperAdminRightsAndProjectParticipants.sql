-- ============================================================================
-- 109: SuperAdmin AccessRights + Reports + ProjectPermissions seed + gip user
-- Idempotent. Run after 108_CorporateAuthRolesAndTokens.sql.
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------------
-- 1. AccessRights Edit for SuperAdmin (RoleId = 1) on all matrix objects
-- ---------------------------------------------------------------------------
DECLARE @SuperAdminRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] IN (N'SuperAdmin', N'Administrator') AND [Id] = 1);
IF @SuperAdminRoleId IS NULL
    SET @SuperAdminRoleId = (SELECT TOP 1 [Id] FROM [Roles] WHERE [Name] IN (N'SuperAdmin', N'Administrator') ORDER BY [Id]);

IF @SuperAdminRoleId IS NOT NULL
BEGIN
    DECLARE @MatrixObjects TABLE ([ObjectName] NVARCHAR(100) NOT NULL);
    INSERT INTO @MatrixObjects ([ObjectName]) VALUES
        (N'Dashboard'), (N'Users'), (N'Projects'), (N'PhysicalVolumes'), (N'PhysicalVolumeTypes'),
        (N'Equipment'), (N'IicWorkDirections'), (N'ProjectAnalysisVersions'),
        (N'IicWorkDirectionNorms'), (N'EngineeringWorkCosts'), (N'UnitsOfMeasurement'),
        (N'ProjectPhysicalVolumes'), (N'ProjectEquipments'), (N'ProjectLaborIntensities'),
        (N'ProjectDataUpload'), (N'Assessment'), (N'Planning'),
        (N'VersionComparison'), (N'GroupAnalysis'), (N'GroupTrend'),
        (N'References'), (N'DataPage'), (N'SettingsPage'), (N'HelpPage'),
        (N'LaborIntensities'), (N'ProjectCharacteristics'), (N'UserActionLog'),
        (N'Reports');

    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
    SELECT @SuperAdminRoleId, m.[ObjectName], 2
    FROM @MatrixObjects m
    WHERE NOT EXISTS (
        SELECT 1 FROM [AccessRights] ar
        WHERE ar.[RoleId] = @SuperAdminRoleId AND ar.[ObjectName] = m.[ObjectName]
    );

    UPDATE ar SET ar.[AccessLevel] = 2
    FROM [AccessRights] ar
    INNER JOIN @MatrixObjects m ON m.[ObjectName] = ar.[ObjectName]
    WHERE ar.[RoleId] = @SuperAdminRoleId AND ar.[AccessLevel] <> 2;
END;

-- ---------------------------------------------------------------------------
-- 2. Reports for Admin / Engineer / Gip
-- ---------------------------------------------------------------------------
DECLARE @AdminRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Admin');
DECLARE @EngineerRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Engineer');
DECLARE @GipRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Gip');

IF @AdminRoleId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [AccessRights] WHERE [RoleId] = @AdminRoleId AND [ObjectName] = N'Reports')
    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel]) VALUES (@AdminRoleId, N'Reports', 2);

IF @EngineerRoleId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [AccessRights] WHERE [RoleId] = @EngineerRoleId AND [ObjectName] = N'Reports')
    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel]) VALUES (@EngineerRoleId, N'Reports', 1);

IF @GipRoleId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [AccessRights] WHERE [RoleId] = @GipRoleId AND [ObjectName] = N'Reports')
    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel]) VALUES (@GipRoleId, N'Reports', 1);

-- ---------------------------------------------------------------------------
-- 3. Demo user gip / gip
-- ---------------------------------------------------------------------------
IF @GipRoleId IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'gip')
BEGIN
    -- Password: gip (BCrypt)
    INSERT INTO [Users] ([Username], [DisplayName], [PasswordHash], [RoleId], [IsActive], [Name], [IsArchived], [CreatedAt])
    VALUES (
        N'gip',
        N'Главный инженер проекта',
        N'$2a$11$tqQRV5MiiA4VRXfbNpT8JegghhDZy7xY4Qbm/sWqfwwe4U3v9Xuqa',
        @GipRoleId,
        1,
        N'Главный инженер проекта',
        0,
        SYSUTCDATETIME()
    );
END;

-- ---------------------------------------------------------------------------
-- 4. ProjectPermissions for demo users on existing projects
-- ---------------------------------------------------------------------------
DECLARE @ManagerUserId INT = (SELECT [Id] FROM [Users] WHERE [Username] = N'manager');
DECLARE @ObserverUserId INT = (SELECT [Id] FROM [Users] WHERE [Username] = N'observer');
DECLARE @GipUserId INT = (SELECT [Id] FROM [Users] WHERE [Username] = N'gip');

IF OBJECT_ID(N'dbo.ProjectPermissions', N'U') IS NOT NULL
BEGIN
    ;WITH TargetProjects AS (
        SELECT [Id] AS ProjectId FROM [Projects] WHERE [IsArchived] = 0
    )
    INSERT INTO [ProjectPermissions] ([ProjectId], [UserId], [Level])
    SELECT p.ProjectId, u.UserId, u.[Level]
    FROM TargetProjects p
    CROSS APPLY (
        SELECT @ManagerUserId AS UserId, 2 AS [Level] WHERE @ManagerUserId IS NOT NULL
        UNION ALL
        SELECT @ObserverUserId, 1 WHERE @ObserverUserId IS NOT NULL
        UNION ALL
        SELECT @GipUserId, 2 WHERE @GipUserId IS NOT NULL
    ) u
    WHERE NOT EXISTS (
        SELECT 1 FROM [ProjectPermissions] pp
        WHERE pp.[ProjectId] = p.ProjectId AND pp.[UserId] = u.UserId
    );
END;

COMMIT;
GO
