IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [PhysicalVolumeTypes] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(4000) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_PhysicalVolumeTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [Projects] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [WorkNumber] nvarchar(100) NULL,
        [ContractNumber] nvarchar(200) NULL,
        [CalendarPlanNumber] nvarchar(200) NULL,
        [Type] nvarchar(200) NULL,
        [Customer] nvarchar(500) NULL,
        [Contractor] nvarchar(500) NULL,
        [Gip] nvarchar(300) NULL,
        [WorkStatus] nvarchar(100) NULL,
        [WorkStart] nvarchar(100) NULL,
        [Program] nvarchar(300) NULL,
        [Visibility] int NOT NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Projects] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(500) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [UnitsOfMeasurement] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(500) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_UnitsOfMeasurement] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [ProjectAnalysisVersions] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [VersionName] nvarchar(200) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ProjectAnalysisVersions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectAnalysisVersions_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [AccessRights] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ObjectName] nvarchar(100) NOT NULL,
        [AccessLevel] int NOT NULL,
        CONSTRAINT [PK_AccessRights] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AccessRights_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(256) NULL,
        [RoleId] int NOT NULL,
        [WindowsUsername] nvarchar(100) NULL,
        [IsActive] bit NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [IicWorkDirections] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Description] nvarchar(2000) NOT NULL,
        [UnitId] int NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_IicWorkDirections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IicWorkDirections_IicWorkDirections_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_IicWorkDirections_UnitsOfMeasurement_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [UnitsOfMeasurement] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [ProjectUserAccess] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_ProjectUserAccess] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectUserAccess_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectUserAccess_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [IicWorkDirectionNorms] (
        [Id] int NOT NULL IDENTITY,
        [WorkDirectionId] int NOT NULL,
        [UnitId] int NULL,
        [EffectiveFrom] datetime2 NOT NULL,
        [EffectiveTo] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_IicWorkDirectionNorms] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IicWorkDirectionNorms_IicWorkDirections_WorkDirectionId] FOREIGN KEY ([WorkDirectionId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_IicWorkDirectionNorms_UnitsOfMeasurement_UnitId] FOREIGN KEY ([UnitId]) REFERENCES [UnitsOfMeasurement] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [PhysicalVolumes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [VolumeTypeId] int NOT NULL,
        [ClassifierId] int NULL,
        [IsAssessment] bit NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_PhysicalVolumes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhysicalVolumes_IicWorkDirections_ClassifierId] FOREIGN KEY ([ClassifierId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PhysicalVolumes_PhysicalVolumeTypes_VolumeTypeId] FOREIGN KEY ([VolumeTypeId]) REFERENCES [PhysicalVolumeTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhysicalVolumes_PhysicalVolumes_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [PhysicalVolumes] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [ProjectLaborIntensities] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [DocumentationCode] nvarchar(100) NOT NULL,
        [WorkDirectionId] int NOT NULL,
        [PlannedHours] decimal(18,4) NOT NULL,
        [ActualHours] decimal(18,4) NOT NULL,
        [Notes] nvarchar(2000) NULL,
        [Brand] nvarchar(100) NULL,
        [ProjectAnalysisVersionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ProjectLaborIntensities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectLaborIntensities_IicWorkDirections_WorkDirectionId] FOREIGN KEY ([WorkDirectionId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectLaborIntensities_ProjectAnalysisVersions_ProjectAnalysisVersionId] FOREIGN KEY ([ProjectAnalysisVersionId]) REFERENCES [ProjectAnalysisVersions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectLaborIntensities_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE TABLE [ProjectPhysicalVolumes] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [PhysicalVolumeId] int NOT NULL,
        [Quantity] decimal(18,4) NOT NULL,
        [FacilityCode] nvarchar(100) NULL,
        [FacilityName] nvarchar(500) NULL,
        [BindingType] nvarchar(200) NULL,
        [DocumentSet] nvarchar(100) NULL,
        [CatalogId] nvarchar(100) NULL,
        [Tag] nvarchar(100) NULL,
        [WorkComment] nvarchar(500) NULL,
        [EstimationBasis] nvarchar(500) NULL,
        [LaborHoursPerUnit] decimal(18,4) NULL,
        [MachineHoursPerUnit] decimal(18,4) NULL,
        [TotalMachineHours] decimal(18,4) NULL,
        [Status] nvarchar(50) NULL,
        [ProjectAnalysisVersionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ProjectPhysicalVolumes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectPhysicalVolumes_PhysicalVolumes_PhysicalVolumeId] FOREIGN KEY ([PhysicalVolumeId]) REFERENCES [PhysicalVolumes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectPhysicalVolumes_ProjectAnalysisVersions_ProjectAnalysisVersionId] FOREIGN KEY ([ProjectAnalysisVersionId]) REFERENCES [ProjectAnalysisVersions] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectPhysicalVolumes_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AccessRights_RoleId_ObjectName] ON [AccessRights] ([RoleId], [ObjectName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IicWorkDirectionNorms_UnitId] ON [IicWorkDirectionNorms] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IicWorkDirectionNorms_WorkDirectionId] ON [IicWorkDirectionNorms] ([WorkDirectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_IicWorkDirections_Code] ON [IicWorkDirections] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IicWorkDirections_ParentId] ON [IicWorkDirections] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_IicWorkDirections_UnitId] ON [IicWorkDirections] ([UnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_ClassifierId] ON [PhysicalVolumes] ([ClassifierId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_Code] ON [PhysicalVolumes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_ParentId] ON [PhysicalVolumes] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_VolumeTypeId] ON [PhysicalVolumes] ([VolumeTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_IsAssessment] ON [PhysicalVolumes] ([IsAssessment]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_PhysicalVolumes_IsArchived] ON [PhysicalVolumes] ([IsArchived]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ProjectAnalysisVersions_ProjectId] ON [ProjectAnalysisVersions] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectLaborIntensities_ProjectAnalysisVersionId_ProjectId_DocumentationCode_WorkDirectionId] ON [ProjectLaborIntensities] ([ProjectAnalysisVersionId], [ProjectId], [DocumentationCode], [WorkDirectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ProjectLaborIntensities_ProjectId] ON [ProjectLaborIntensities] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ProjectLaborIntensities_WorkDirectionId] ON [ProjectLaborIntensities] ([WorkDirectionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ProjectPhysicalVolumes_PhysicalVolumeId] ON [ProjectPhysicalVolumes] ([PhysicalVolumeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId] ON [ProjectPhysicalVolumes] ([ProjectAnalysisVersionId], [ProjectId], [PhysicalVolumeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ProjectPhysicalVolumes_ProjectId] ON [ProjectPhysicalVolumes] ([ProjectId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Projects_Code] ON [Projects] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProjectUserAccess_ProjectId_UserId] ON [ProjectUserAccess] ([ProjectId], [UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_ProjectUserAccess_UserId] ON [ProjectUserAccess] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_UnitsOfMeasurement_Name] ON [UnitsOfMeasurement] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE INDEX [IX_Users_RoleId] ON [Users] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Users_WindowsUsername] ON [Users] ([WindowsUsername]) WHERE WindowsUsername IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260411092503_InitialSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260411092503_InitialSchema', N'10.0.5');
END;

COMMIT;
GO

