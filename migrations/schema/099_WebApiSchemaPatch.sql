-- Идемпотентный патч: закрывает drift между sqlcmd-цепочкой и backend EF-моделью (V2A+).
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.Users', 'WindowsSid') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [WindowsSid] nvarchar(64) NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_WindowsSid' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE UNIQUE INDEX [IX_Users_WindowsSid] ON [Users]([WindowsSid]) WHERE [WindowsSid] IS NOT NULL;
END;
GO

IF COL_LENGTH('dbo.Projects', 'ContractDate') IS NULL
    ALTER TABLE [Projects] ADD [ContractDate] date NULL;
GO

IF COL_LENGTH('dbo.Projects', 'WorkEnd') IS NULL
    ALTER TABLE [Projects] ADD [WorkEnd] date NULL;
GO

IF COL_LENGTH('dbo.Projects', 'WorkStart') IS NULL
    ALTER TABLE [Projects] ADD [WorkStart] date NULL;
GO

-- V2-A.1: WorkStart был nvarchar(100) в 001_InitialSchema — конвертируем в date для EF DateOnly.
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'Projects'
      AND COLUMN_NAME = N'WorkStart' AND DATA_TYPE = N'nvarchar'
)
BEGIN
    UPDATE [Projects]
    SET [WorkStart] = NULL
    WHERE [WorkStart] IS NOT NULL
      AND (LTRIM(RTRIM([WorkStart])) = N''
           OR TRY_CONVERT(date, [WorkStart], 23) IS NULL);

    ALTER TABLE [Projects] ALTER COLUMN [WorkStart] date NULL;
END;
GO

IF OBJECT_ID(N'dbo.UserSettings', N'U') IS NULL
BEGIN
    CREATE TABLE [UserSettings] (
        [Id]     INT            NOT NULL IDENTITY,
        [UserId] INT            NOT NULL,
        [Key]    NVARCHAR(200)  NOT NULL,
        [Value]  NVARCHAR(MAX)  NOT NULL,
        CONSTRAINT [PK_UserSettings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSettings_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_UserSettings_UserId_Key] ON [UserSettings]([UserId], [Key]);
    CREATE INDEX [IX_UserSettings_UserId] ON [UserSettings]([UserId]);
END;
GO

IF OBJECT_ID(N'dbo.UserActionLogs', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[UserActionLogs] (
        [Id]          INT             IDENTITY(1,1) NOT NULL,
        [UserId]      INT             NOT NULL,
        [Timestamp]   DATETIME2(7)    NOT NULL,
        [ActionType]  NVARCHAR(64)    NOT NULL,
        [EntityType]  NVARCHAR(100)   NULL,
        [EntityId]    INT             NULL,
        [Description] NVARCHAR(500)   NULL,
        [IpOrHost]    NVARCHAR(64)    NULL,
        CONSTRAINT [PK_UserActionLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserActionLogs_Users_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_UserActionLogs_UserId_Timestamp]
        ON [dbo].[UserActionLogs]([UserId] ASC, [Timestamp] ASC);
    CREATE INDEX [IX_UserActionLogs_ActionType]
        ON [dbo].[UserActionLogs]([ActionType] ASC);
    CREATE INDEX [IX_UserActionLogs_EntityType_EntityId]
        ON [dbo].[UserActionLogs]([EntityType] ASC, [EntityId] ASC);
END;
GO

IF COL_LENGTH('dbo.UserActionLogs', 'IsArchived') IS NULL
BEGIN
    ALTER TABLE [dbo].[UserActionLogs] ADD [IsArchived] bit NOT NULL
        CONSTRAINT [DF_UserActionLogs_IsArchived] DEFAULT 0;
END;
GO

IF COL_LENGTH('dbo.DataImportIterations', 'ImportedByUserId') IS NULL
    ALTER TABLE [DataImportIterations] ADD [ImportedByUserId] int NULL;
GO

IF COL_LENGTH('dbo.DataImportIterations', 'SourceFileHash') IS NULL
    ALTER TABLE [DataImportIterations] ADD [SourceFileHash] nchar(64) NULL;
GO

IF COL_LENGTH('dbo.DataImportIterations', 'SourceFileName') IS NULL
    ALTER TABLE [DataImportIterations] ADD [SourceFileName] nvarchar(500) NULL;
GO

IF OBJECT_ID(N'dbo.AnalysisVersionSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE [AnalysisVersionSnapshots] (
        [Id] int NOT NULL IDENTITY,
        [ProjectAnalysisVersionId] int NOT NULL,
        [WorkDirectionId] int NOT NULL,
        [NormId] int NOT NULL,
        CONSTRAINT [PK_AnalysisVersionSnapshots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AnalysisVersionSnapshots_ProjectAnalysisVersions_ProjectAnalysisVersionId]
            FOREIGN KEY ([ProjectAnalysisVersionId]) REFERENCES [ProjectAnalysisVersions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AnalysisVersionSnapshots_IicWorkDirections_WorkDirectionId]
            FOREIGN KEY ([WorkDirectionId]) REFERENCES [IicWorkDirections]([Id]),
        CONSTRAINT [FK_AnalysisVersionSnapshots_IicWorkDirectionNorms_NormId]
            FOREIGN KEY ([NormId]) REFERENCES [IicWorkDirectionNorms]([Id])
    );
    CREATE UNIQUE INDEX [UX_AnalysisVersionSnapshots_Version_WorkDirection]
        ON [AnalysisVersionSnapshots]([ProjectAnalysisVersionId], [WorkDirectionId]);
END;
GO

IF OBJECT_ID(N'dbo.ProjectPermissions', N'U') IS NULL
BEGIN
    CREATE TABLE [ProjectPermissions] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [UserId] int NOT NULL,
        [Level] int NOT NULL CONSTRAINT [DF_ProjectPermissions_Level] DEFAULT 1,
        CONSTRAINT [PK_ProjectPermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectPermissions_Projects_ProjectId]
            FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectPermissions_Users_UserId]
            FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [UX_ProjectPermissions_Project_User] ON [ProjectPermissions]([ProjectId], [UserId]);
END;
GO

-- V2-C: ProjectPermissions заменяет ProjectUserAccess + Projects.Visibility (идемпотентно).
-- Dynamic SQL: иначе компилятор падает на Visibility, если колонка уже удалена.
IF COL_LENGTH('dbo.Projects', 'Visibility') IS NOT NULL
BEGIN
    EXEC(N'
        IF OBJECT_ID(N''dbo.ProjectUserAccess'', N''U'') IS NOT NULL
        BEGIN
            INSERT INTO [ProjectPermissions] ([ProjectId], [UserId], [Level])
            SELECT DISTINCT pua.[ProjectId], pua.[UserId], 1
            FROM [ProjectUserAccess] pua
            INNER JOIN [Projects] p ON p.[Id] = pua.[ProjectId]
            WHERE p.[Visibility] = 1
              AND NOT EXISTS (
                  SELECT 1 FROM [ProjectPermissions] pp
                  WHERE pp.[ProjectId] = pua.[ProjectId] AND pp.[UserId] = pua.[UserId]);

            INSERT INTO [ProjectPermissions] ([ProjectId], [UserId], [Level])
            SELECT p.[Id], u.[Id], 1
            FROM [Projects] p
            CROSS JOIN [Users] u
            INNER JOIN [Roles] r ON r.[Id] = u.[RoleId]
            WHERE p.[Visibility] = 0
              AND r.[Name] <> N''Administrator''
              AND NOT EXISTS (
                  SELECT 1 FROM [ProjectPermissions] pp
                  WHERE pp.[ProjectId] = p.[Id] AND pp.[UserId] = u.[Id]);

            DROP TABLE [ProjectUserAccess];
        END;

        IF COL_LENGTH(''dbo.Projects'', ''Visibility'') IS NOT NULL
            ALTER TABLE [Projects] DROP COLUMN [Visibility];
    ');
END;
GO

IF OBJECT_ID(N'dbo.Equipments', N'U') IS NULL
BEGIN
    CREATE TABLE [Equipments] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(4000) NOT NULL,
        [ClassifierId] int NULL,
        [IsAssessment] bit NULL,
        [Coefficient] decimal(18,4) NULL,
        [Name] nvarchar(1000) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        [UnitId] int NULL,
        CONSTRAINT [PK_Equipments] PRIMARY KEY ([Id])
    );
END;
GO

IF OBJECT_ID(N'dbo.ProjectEquipments', N'U') IS NULL
BEGIN
    CREATE TABLE [ProjectEquipments] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [EquipmentId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [QuantityInWork] decimal(18,4) NULL,
        [PositionNumber] nvarchar(50) NULL,
        [FacilityCode] nvarchar(100) NULL,
        [FacilityName] nvarchar(500) NULL,
        [DocumentSet] nvarchar(100) NULL,
        [DiagramNumber] nvarchar(200) NULL,
        [ShortName] nvarchar(1000) NULL,
        [CatalogName] nvarchar(1000) NULL,
        [TypeMark] nvarchar(200) NULL,
        [CatalogId] nvarchar(100) NULL,
        [Tag] nvarchar(100) NULL,
        [FullEquipmentId] nvarchar(100) NULL,
        [SupplyStandard] nvarchar(200) NULL,
        [OlReference] nvarchar(200) NULL,
        [EstimationBasis] nvarchar(2000) NULL,
        [ProjectAnalysisVersionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ProjectEquipments] PRIMARY KEY ([Id])
    );
END;
GO

-- PhysicalVolumes.UnitId (EF AddPhysicalVolumeUnit) — legacy table removed by schema 017
IF OBJECT_ID(N'dbo.PhysicalVolumes', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PhysicalVolumes', 'UnitId') IS NULL
BEGIN
    ALTER TABLE [PhysicalVolumes] ADD [UnitId] int NULL;
    CREATE INDEX [IX_PhysicalVolumes_UnitId] ON [PhysicalVolumes]([UnitId]);
    IF OBJECT_ID(N'dbo.UnitsOfMeasurement', N'U') IS NOT NULL
        ALTER TABLE [PhysicalVolumes] ADD CONSTRAINT [FK_PhysicalVolumes_UnitsOfMeasurement_UnitId]
            FOREIGN KEY ([UnitId]) REFERENCES [UnitsOfMeasurement]([Id]) ON DELETE SET NULL;
END;
GO

-- PhysicalVolumes.IicNormId (каскад КФО → нормы ИИС)
IF OBJECT_ID(N'dbo.PhysicalVolumes', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PhysicalVolumes', 'IicNormId') IS NULL
BEGIN
    ALTER TABLE [PhysicalVolumes] ADD [IicNormId] int NULL;
END;
GO

IF OBJECT_ID(N'dbo.PhysicalVolumes', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PhysicalVolumes', 'IicNormId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_PhysicalVolumes_IicNormId' AND object_id = OBJECT_ID(N'dbo.PhysicalVolumes')
   )
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_IicNormId] ON [PhysicalVolumes]([IicNormId]);
END;
GO

IF OBJECT_ID(N'dbo.PhysicalVolumes', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.PhysicalVolumes', 'IicNormId') IS NOT NULL
   AND OBJECT_ID(N'dbo.IicWorkDirectionNorms', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys
       WHERE name = N'FK_PhysicalVolumes_IicWorkDirectionNorms_IicNormId'
   )
BEGIN
    -- NO ACTION: иначе SQL Server ругается на multiple cascade paths.
    ALTER TABLE [PhysicalVolumes] ADD CONSTRAINT [FK_PhysicalVolumes_IicWorkDirectionNorms_IicNormId]
        FOREIGN KEY ([IicNormId]) REFERENCES [IicWorkDirectionNorms]([Id]) ON DELETE NO ACTION;
END;
GO

IF COL_LENGTH('dbo.Equipments', 'CatalogKeyHash') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipments_Name' AND object_id = OBJECT_ID(N'dbo.Equipments'))
        DROP INDEX [IX_Equipments_Name] ON [Equipments];
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipments_Name_UnitId' AND object_id = OBJECT_ID(N'dbo.Equipments'))
        DROP INDEX [IX_Equipments_Name_UnitId] ON [Equipments];

    ALTER TABLE [Equipments] ADD [CatalogKeyHash] nvarchar(64) NOT NULL
        CONSTRAINT [DF_Equipments_CatalogKeyHash] DEFAULT N'' WITH VALUES;
END;
GO

IF COL_LENGTH('dbo.Equipments', 'CatalogKeyHash') IS NOT NULL
BEGIN
    UPDATE e
    SET [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT([Name], CHAR(0), COALESCE(CAST([UnitId] AS nvarchar(20)), N''))), 2))
    FROM [Equipments] e
    WHERE [CatalogKeyHash] = N'';

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipments_CatalogKeyHash' AND object_id = OBJECT_ID(N'dbo.Equipments'))
        CREATE UNIQUE INDEX [IX_Equipments_CatalogKeyHash] ON [Equipments]([CatalogKeyHash]);
END;
GO

-- LaborIntensities.DocumentTypeName / Techas (EF SyncBeforeV2 + AddLaborIntensityTechas)
IF COL_LENGTH('dbo.LaborIntensities', 'DocumentTypeName') IS NULL
BEGIN
    ALTER TABLE [LaborIntensities] ADD [DocumentTypeName] nvarchar(500) NULL;
    CREATE INDEX [IX_LaborIntensities_DocumentTypeName] ON [LaborIntensities]([DocumentTypeName]);
END;
GO

IF COL_LENGTH('dbo.LaborIntensities', 'Techas') IS NULL
    ALTER TABLE [LaborIntensities] ADD [Techas] nvarchar(20) NULL;
GO

IF COL_LENGTH('dbo.ProjectLaborIntensities', 'LaborIntensityId') IS NULL
BEGIN
    ALTER TABLE [ProjectLaborIntensities] ADD [LaborIntensityId] int NULL;
    CREATE INDEX [IX_ProjectLaborIntensities_LaborIntensityId] ON [ProjectLaborIntensities]([LaborIntensityId]);
    IF OBJECT_ID(N'dbo.LaborIntensities', N'U') IS NOT NULL
        ALTER TABLE [ProjectLaborIntensities] ADD CONSTRAINT [FK_ProjectLaborIntensities_LaborIntensities_LaborIntensityId]
            FOREIGN KEY ([LaborIntensityId]) REFERENCES [LaborIntensities]([Id]) ON DELETE SET NULL;
END;
GO

-- ProjectAnalysisVersions.Description (EF AddProjectAnalysisVersionDescription)
IF COL_LENGTH('dbo.ProjectAnalysisVersions', 'Description') IS NULL
    ALTER TABLE [ProjectAnalysisVersions] ADD [Description] nvarchar(2000) NULL;
GO

-- ProjectPhysicalVolumes: work/catalog fields + иерархия ParentId (backend V2)
IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'CatalogCode') IS NULL
    ALTER TABLE [ProjectPhysicalVolumes] ADD [CatalogCode] nvarchar(100) NULL;
GO

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'CatalogName') IS NULL
    ALTER TABLE [ProjectPhysicalVolumes] ADD [CatalogName] nvarchar(1000) NULL;
GO

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'WorkId') IS NULL
    ALTER TABLE [ProjectPhysicalVolumes] ADD [WorkId] nvarchar(200) NULL;
GO

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'WorkName') IS NULL
    ALTER TABLE [ProjectPhysicalVolumes] ADD [WorkName] nvarchar(1000) NULL;
GO

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'ParentId') IS NULL
BEGIN
    ALTER TABLE [ProjectPhysicalVolumes] ADD [ParentId] int NULL;
    CREATE INDEX [IX_ProjectPhysicalVolumes_ParentId] ON [ProjectPhysicalVolumes]([ParentId]);
    ALTER TABLE [ProjectPhysicalVolumes] ADD CONSTRAINT [FK_ProjectPhysicalVolumes_ProjectPhysicalVolumes_ParentId]
        FOREIGN KEY ([ParentId]) REFERENCES [ProjectPhysicalVolumes]([Id]) ON DELETE NO ACTION;
END;
GO

-- ProjectLaborIntensities.ProjectPhysicalVolumeId (backend deep copy / fact grid)
IF COL_LENGTH('dbo.ProjectLaborIntensities', 'ProjectPhysicalVolumeId') IS NULL
    ALTER TABLE [ProjectLaborIntensities] ADD [ProjectPhysicalVolumeId] int NULL;
GO

IF COL_LENGTH('dbo.ProjectLaborIntensities', 'ProjectPhysicalVolumeId') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectLaborIntensities_ProjectPhysicalVolumeId' AND object_id = OBJECT_ID(N'dbo.ProjectLaborIntensities'))
        CREATE INDEX [IX_ProjectLaborIntensities_ProjectPhysicalVolumeId] ON [ProjectLaborIntensities]([ProjectPhysicalVolumeId]);

    -- NO ACTION: иначе SQL Server ругается на multiple cascade paths (версия → PPV → PLI).
    -- NO ACTION: SQL Server отклоняет даже SET NULL из-за multiple cascade paths
    -- (версия CASCADE → PPV и версия CASCADE → PLI).
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectLaborIntensities_ProjectPhysicalVolumes_ProjectPhysicalVolumeId')
        ALTER TABLE [ProjectLaborIntensities] ADD CONSTRAINT [FK_ProjectLaborIntensities_ProjectPhysicalVolumes_ProjectPhysicalVolumeId]
            FOREIGN KEY ([ProjectPhysicalVolumeId]) REFERENCES [ProjectPhysicalVolumes]([Id]) ON DELETE NO ACTION;
END;
GO

-- ProjectLaborIntensities.WorkDirectionId → nullable (EF/WPF 20260504121942)
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'ProjectLaborIntensities'
      AND COLUMN_NAME = N'WorkDirectionId' AND IS_NULLABLE = N'NO'
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId' AND object_id = OBJECT_ID(N'dbo.ProjectLaborIntensities'))
        DROP INDEX [IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId] ON [ProjectLaborIntensities];

    ALTER TABLE [ProjectLaborIntensities] ALTER COLUMN [WorkDirectionId] int NULL;

    CREATE UNIQUE INDEX [IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId]
        ON [ProjectLaborIntensities]([ProjectAnalysisVersionId], [ProjectId], [DocumentationCode], [WorkDirectionId])
        WHERE [WorkDirectionId] IS NOT NULL;
END;
GO

-- PhysicalVolumeId ON DELETE SET NULL (EF V2D)
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId')
BEGIN
    ALTER TABLE [ProjectLaborIntensities] DROP CONSTRAINT [FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId];
    ALTER TABLE [ProjectLaborIntensities] ADD CONSTRAINT [FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId]
        FOREIGN KEY ([PhysicalVolumeId]) REFERENCES [PhysicalVolumes]([Id]) ON DELETE SET NULL;
END;
GO

-- DataImportIterations.ImportedByUserId → Users
IF COL_LENGTH('dbo.DataImportIterations', 'ImportedByUserId') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DataImportIterations_ImportedByUserId' AND object_id = OBJECT_ID(N'dbo.DataImportIterations'))
        CREATE INDEX [IX_DataImportIterations_ImportedByUserId] ON [DataImportIterations]([ImportedByUserId]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DataImportIterations_Users_ImportedByUserId')
        ALTER TABLE [DataImportIterations] ADD CONSTRAINT [FK_DataImportIterations_Users_ImportedByUserId]
            FOREIGN KEY ([ImportedByUserId]) REFERENCES [Users]([Id]) ON DELETE SET NULL;
END;
GO

-- Equipments: FK + индексы (EF AddEquipment)
IF OBJECT_ID(N'dbo.Equipments', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipments_ClassifierId' AND object_id = OBJECT_ID(N'dbo.Equipments'))
        CREATE INDEX [IX_Equipments_ClassifierId] ON [Equipments]([ClassifierId]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipments_ParentId' AND object_id = OBJECT_ID(N'dbo.Equipments'))
        CREATE INDEX [IX_Equipments_ParentId] ON [Equipments]([ParentId]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipments_UnitId' AND object_id = OBJECT_ID(N'dbo.Equipments'))
        CREATE INDEX [IX_Equipments_UnitId] ON [Equipments]([UnitId]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Equipments_IicWorkDirections_ClassifierId')
        ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_IicWorkDirections_ClassifierId]
            FOREIGN KEY ([ClassifierId]) REFERENCES [IicWorkDirections]([Id]) ON DELETE SET NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Equipments_Equipments_ParentId')
        ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_Equipments_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [Equipments]([Id]) ON DELETE NO ACTION;

    IF COL_LENGTH('dbo.Equipments', 'UnitId') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Equipments_UnitsOfMeasurement_UnitId')
        ALTER TABLE [Equipments] ADD CONSTRAINT [FK_Equipments_UnitsOfMeasurement_UnitId]
            FOREIGN KEY ([UnitId]) REFERENCES [UnitsOfMeasurement]([Id]) ON DELETE SET NULL;
END;
GO

-- ProjectEquipments: FK + индексы
IF OBJECT_ID(N'dbo.ProjectEquipments', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectEquipments_EquipmentId' AND object_id = OBJECT_ID(N'dbo.ProjectEquipments'))
        CREATE INDEX [IX_ProjectEquipments_EquipmentId] ON [ProjectEquipments]([EquipmentId]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectEquipments_ProjectId' AND object_id = OBJECT_ID(N'dbo.ProjectEquipments'))
        CREATE INDEX [IX_ProjectEquipments_ProjectId] ON [ProjectEquipments]([ProjectId]);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectEquipments_ProjectAnalysisVersionId' AND object_id = OBJECT_ID(N'dbo.ProjectEquipments'))
        CREATE INDEX [IX_ProjectEquipments_ProjectAnalysisVersionId] ON [ProjectEquipments]([ProjectAnalysisVersionId]);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectEquipments_Equipments_EquipmentId')
        ALTER TABLE [ProjectEquipments] ADD CONSTRAINT [FK_ProjectEquipments_Equipments_EquipmentId]
            FOREIGN KEY ([EquipmentId]) REFERENCES [Equipments]([Id]) ON DELETE NO ACTION;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectEquipments_ProjectAnalysisVersions_ProjectAnalysisVersionId')
        ALTER TABLE [ProjectEquipments] ADD CONSTRAINT [FK_ProjectEquipments_ProjectAnalysisVersions_ProjectAnalysisVersionId]
            FOREIGN KEY ([ProjectAnalysisVersionId]) REFERENCES [ProjectAnalysisVersions]([Id]) ON DELETE CASCADE;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectEquipments_Projects_ProjectId')
        ALTER TABLE [ProjectEquipments] ADD CONSTRAINT [FK_ProjectEquipments_Projects_ProjectId]
            FOREIGN KEY ([ProjectId]) REFERENCES [Projects]([Id]) ON DELETE NO ACTION;
END;
GO

-- WPF parity: ExpandEquipmentTextFields — длинные «Наименование по каталогу» из Excel.
IF OBJECT_ID(N'dbo.Equipments', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [Equipments] ALTER COLUMN [Name] nvarchar(max) NOT NULL;
    ALTER TABLE [Equipments] ALTER COLUMN [Description] nvarchar(max) NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.ProjectEquipments', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [CatalogName] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [ShortName] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [TypeMark] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [SupplyStandard] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [OlReference] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [FacilityName] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [EstimationBasis] nvarchar(max) NULL;
    ALTER TABLE [ProjectEquipments] ALTER COLUMN [DiagramNumber] nvarchar(max) NULL;
END;
GO

PRINT N'WebApi schema patch applied.';
GO
