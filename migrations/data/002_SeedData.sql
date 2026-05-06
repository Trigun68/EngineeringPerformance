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
IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [Name] = N'Administrator')
BEGIN
    INSERT INTO [Roles] ([Name], [Description], [IsArchived], [CreatedAt])
    VALUES
        (N'Administrator', N'Полный доступ ко всем функциям системы', 0, SYSUTCDATETIME()),
        (N'ProjectManager', N'Управление проектами и данными', 0, SYSUTCDATETIME()),
        (N'Observer', N'Только чтение', 0, SYSUTCDATETIME());
END;

-- ============================================================================
-- 2. Users
-- Passwords: admin / manager / observer / eliseev: 610501 (BCrypt hashed)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'admin')
BEGIN
    DECLARE @AdminRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Administrator');
    DECLARE @ManagerRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'ProjectManager');
    DECLARE @ObserverRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Observer');

    INSERT INTO [Users] ([Username], [DisplayName], [PasswordHash], [RoleId], [IsActive], [Name], [IsArchived], [CreatedAt])
    VALUES
        (N'admin', N'Администратор', N'$2a$11$cqVHZHqoOKmoJJX1YOOkl.qWr9sXLJY527PvPF9cdClnFzVnUZyCW', @AdminRoleId, 1, N'Администратор', 0, SYSUTCDATETIME()),
        (N'manager', N'Менеджер проекта', N'$2a$11$DAp0/fYH.MSg5btb6kZu8u2kdiVBkTQGoVoJJAzcz1.EZv8nAuNOW', @ManagerRoleId, 1, N'Менеджер проекта', 0, SYSUTCDATETIME()),
        (N'observer', N'Наблюдатель', N'$2a$11$xOY29c1EDd/Pt.EeLhdXMuTD0/QxleNrMwRMPdcCL0pFLWnsICDQO', @ObserverRoleId, 1, N'Наблюдатель', 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [Users] WHERE [Username] = N'eliseev')
BEGIN
    DECLARE @EliseevRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Administrator');

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
-- 7. Projects (PRJ-25-310, PRJ-25-970) — только карточки, без версий анализа и доступов
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [Code] = N'PRJ-25-310')
BEGIN
    SET IDENTITY_INSERT [Projects] ON;

    INSERT INTO [Projects] ([Id], [Code], [WorkNumber], [ContractNumber], [CalendarPlanNumber],
                            [Type], [Customer], [Contractor], [Gip], [WorkStatus], [WorkStart],
                            [Program], [Visibility], [Name], [ParentId], [IsArchived], [CreatedAt], [ModifiedAt], [SortOrder])
    VALUES
        (4, N'PRJ-25-310', N'25-310', N'', N'', N'', N'', N'', N'', N'', N'', N'', 2,
         N'Программа увеличения производства горячекатаного проката', NULL, 0,
         CAST(N'2026-04-11T20:00:11.7633632' AS datetime2), CAST(N'2026-04-16T19:34:13.3243627' AS datetime2), 0),
        (5, N'PRJ-25-970', N'25-970', N'', N'', N'', N'', N'', N'Вася', N'в работе', N'', N'', 1,
         N'Программа Новый горизонт. Строительство комплекса полного цикла по производству DR-окатышей', NULL, 0,
         CAST(N'2026-04-11T20:00:11.7633632' AS datetime2), CAST(N'2026-04-16T20:32:49.3155543' AS datetime2), 1);

    SET IDENTITY_INSERT [Projects] OFF;
END;

-- ============================================================================
-- 8. Access Rights
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM [AccessRights] WHERE [RoleId] = (SELECT [Id] FROM [Roles] WHERE [Name] = N'ProjectManager'))
BEGIN
    DECLARE @PmRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'ProjectManager');
    DECLARE @ObsRoleId INT = (SELECT [Id] FROM [Roles] WHERE [Name] = N'Observer');

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

COMMIT;
GO
