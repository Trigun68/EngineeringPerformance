-- ============================================================================
-- Seed Data: Engineering Performance Management System
-- ============================================================================
-- This script inserts test/demo data into the database.
-- It is idempotent — safe to run multiple times.
-- Run AFTER applying the schema migration (001_InitialSchema.sql).
-- ============================================================================

BEGIN TRANSACTION;

-- ============================================================================
-- 1. Roles
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'SuperAdmin')
BEGIN
    SET IDENTITY_INSERT [Roles] ON;

    INSERT INTO [Roles] ([Id], [Name], [Description], [IsArchived], [CreatedAt])
    VALUES
        (1, N'SuperAdmin', N'Полный доступ ко всем функциям системы', 0, SYSUTCDATETIME()),
        (2, N'Admin', N'Управление пользователями и настройками', 0, SYSUTCDATETIME()),
        (3, N'Engineer', N'Работа с проектами и данными', 0, SYSUTCDATETIME());

    SET IDENTITY_INSERT [Roles] OFF;
END;

-- ============================================================================
-- 2. Users
-- Passwords (initial seed only; NOT reset on re-run):
--   admin / admin, manager / manager, observer / observer
-- BCrypt hashes below. Do not UPDATE PasswordHash on every db-init — that wipes user password changes.
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'admin')
BEGIN
    DECLARE @AdminRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'SuperAdmin');
    DECLARE @ManagerRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Admin');
    DECLARE @ObserverRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Engineer');

    INSERT INTO [Users] ([Username], [DisplayName], [PasswordHash], [RoleId], [IsActive], [Name], [IsArchived], [CreatedAt])
    VALUES
        (N'admin', N'Администратор', N'$2a$11$cqVHZHqoOKmoJJX1YOOkl.qWr9sXLJY527PvPF9cdClnFzVnUZyCW', @AdminRoleId, 1, N'Администратор', 0, SYSUTCDATETIME()),
        (N'manager', N'Менеджер проекта', N'$2a$11$DAp0/fYH.MSg5btb6kZu8u2kdiVBkTQGoVoJJAzcz1.EZv8nAuNOW', @ManagerRoleId, 1, N'Менеджер проекта', 0, SYSUTCDATETIME()),
        (N'observer', N'Наблюдатель', N'$2a$11$xOY29c1EDd/Pt.EeLhdXMuTD0/QxleNrMwRMPdcCL0pFLWnsICDQO', @ObserverRoleId, 1, N'Наблюдатель', 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'eliseev')
BEGIN
    DECLARE @EliseevRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'SuperAdmin');

    INSERT INTO [Users] ([Username], [DisplayName], [PasswordHash], [RoleId], [WindowsUsername], [IsActive], [Name], [IsArchived], [CreatedAt])
    VALUES
        (N'eliseev', N'Елисеев', N'$2a$11$7p1qpOzzvVARhzRliu56Cekth5Ky2le.zj8mzO4jNQezB66VNsaMm', @EliseevRoleId, N'LAPTOP-ANTON\elise', 1, N'Елисеев', 0, SYSUTCDATETIME());
END;

-- ============================================================================
-- 3. Units of Measurement
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м³')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [IsArchived], [CreatedAt])
    VALUES
        (N'м³', N'Кубический метр', 0, SYSUTCDATETIME()),
        (N'м²', N'Квадратный метр', 0, SYSUTCDATETIME()),
        (N'шт', N'Штука', 0, SYSUTCDATETIME()),
        (N'т', N'Тонна', 0, SYSUTCDATETIME()),
        (N'кг', N'Килограмм', 0, SYSUTCDATETIME()),
        (N'час', N'Час', 0, SYSUTCDATETIME()),
        (N'ярд³', N'Кубический ярд', 0, SYSUTCDATETIME()),
        (N'компл.', N'Комплект', 0, SYSUTCDATETIME()),
        (N'км', N'Километр', 0, SYSUTCDATETIME()),
        (N'м', N'Метр', 0, SYSUTCDATETIME()),
        (N'га', N'Гектар', 0, SYSUTCDATETIME()),
        (N'п.м.', N'Погонный метр', 0, SYSUTCDATETIME());
END;

-- ============================================================================
-- 4. Physical Volume Types
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumeTypes] WHERE [Name] = N'БЭР')
BEGIN
    INSERT INTO [PhysicalVolumeTypes] ([Name], [Description], [IsArchived], [CreatedAt])
    VALUES (N'БЭР', N'Базовая единица ресурса', 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumeTypes] WHERE [Name] = N'КФО')
BEGIN
    INSERT INTO [PhysicalVolumeTypes] ([Name], [Description], [IsArchived], [CreatedAt])
    VALUES (N'КФО', N'Классификатор физических объёмов', 0, SYSUTCDATETIME());
END;

-- ============================================================================
-- 5. IIC Work Directions и 6. IIC Work Direction Norms — перенесены в 003_SeedDataFromExcel.sql
--    (источник: лист «Техас», файл «Копия ++++-1.xlsx»).
-- ============================================================================

-- ============================================================================
-- 7. Projects — демо-карточки не сидируются; проекты создаются пользователями в UI.
-- ============================================================================

-- ============================================================================
-- 8. Access Rights
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [AccessRights] WHERE [RoleId] = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Admin'))
BEGIN
    DECLARE @PmRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Admin');
    DECLARE @ObsRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Engineer');

    DECLARE @Objects TABLE ([Name] NVARCHAR(100));
    INSERT INTO @Objects ([Name])
    VALUES
        (N'Users'), (N'Projects'), (N'PhysicalVolumes'), (N'PhysicalVolumeTypes'),
        (N'IicWorkDirections'), (N'ProjectAnalysisVersions'),
        (N'IicWorkDirectionNorms'),
        (N'ProjectPhysicalVolumes'), (N'ProjectLaborIntensities'),
        (N'ProjectDataUpload'), (N'Assessment'), (N'Planning'),
        (N'VersionComparison'), (N'GroupAnalysis'), (N'GroupTrend'),
        (N'LaborIntensities');

    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
    SELECT @PmRoleId, [Name], 2 FROM @Objects;  -- 2 = Edit

    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
    SELECT @ObsRoleId, [Name], 1 FROM @Objects;  -- 1 = Read
END;

DECLARE @GipRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Gip');

IF @GipRoleId IS NOT NULL
BEGIN
    INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
    SELECT @GipRoleId, source.[ObjectName], source.[AccessLevel]
    FROM [AccessRights] source
    WHERE source.[RoleId] = 3
      AND NOT EXISTS (
          SELECT 1
          FROM [AccessRights] target
          WHERE target.[RoleId] = @GipRoleId
            AND target.[ObjectName] = source.[ObjectName]
      );

    UPDATE [AccessRights]
    SET [AccessLevel] = 2
    WHERE [RoleId] = @GipRoleId
      AND [ObjectName] IN (N'Projects', N'Planning', N'Assessment', N'ProjectDataUpload')
      AND [AccessLevel] <> 2;
END;

COMMIT;
GO
