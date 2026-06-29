-- ============================================================================
-- Schema 017: Split PhysicalVolumes into PhysicalVolumesKfo and PhysicalVolumesBer
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesKfo')
BEGIN
    CREATE TABLE [PhysicalVolumesKfo] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [ClassifierId] int NULL,
        [IicNormId] int NULL,
        [IsAssessment] bit NULL,
        [DocumentSet] nvarchar(100) NULL,
        [Coefficient] decimal(18,4) NOT NULL CONSTRAINT [DF_PhysicalVolumesKfo_Coefficient] DEFAULT 1,
        [UnitId] int NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL CONSTRAINT [DF_PhysicalVolumesKfo_SortOrder] DEFAULT 0,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_PhysicalVolumesKfo] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhysicalVolumesKfo_IicWorkDirections_ClassifierId]
            FOREIGN KEY ([ClassifierId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PhysicalVolumesKfo_PhysicalVolumesKfo_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [PhysicalVolumesKfo] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhysicalVolumesKfo_UnitsOfMeasurement_UnitId]
            FOREIGN KEY ([UnitId]) REFERENCES [UnitsOfMeasurement] ([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_PhysicalVolumesKfo_Code] ON [PhysicalVolumesKfo]([Code]);
    CREATE INDEX [IX_PhysicalVolumesKfo_ClassifierId] ON [PhysicalVolumesKfo]([ClassifierId]);
    CREATE INDEX [IX_PhysicalVolumesKfo_ParentId] ON [PhysicalVolumesKfo]([ParentId]);
    CREATE INDEX [IX_PhysicalVolumesKfo_IsArchived] ON [PhysicalVolumesKfo]([IsArchived]);
    CREATE INDEX [IX_PhysicalVolumesKfo_IsAssessment] ON [PhysicalVolumesKfo]([IsAssessment]);
    CREATE INDEX [IX_PhysicalVolumesKfo_DocumentSet] ON [PhysicalVolumesKfo]([DocumentSet]);
    CREATE INDEX [IX_PhysicalVolumesKfo_UnitId] ON [PhysicalVolumesKfo]([UnitId]);
    CREATE INDEX [IX_PhysicalVolumesKfo_IicNormId] ON [PhysicalVolumesKfo]([IicNormId]);
    CREATE INDEX [IX_PhysicalVolumesKfo_IsArchived_ParentId_SortOrder]
        ON [PhysicalVolumesKfo]([IsArchived], [ParentId], [SortOrder]);
    CREATE INDEX [IX_PhysicalVolumesKfo_IsArchived_SortOrder_Name]
        ON [PhysicalVolumesKfo]([IsArchived], [SortOrder], [Name]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumesBer')
BEGIN
    CREATE TABLE [PhysicalVolumesBer] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [ClassifierId] int NULL,
        [IsAssessment] bit NULL,
        [DocumentSet] nvarchar(100) NULL,
        [Coefficient] decimal(18,4) NOT NULL CONSTRAINT [DF_PhysicalVolumesBer_Coefficient] DEFAULT 1,
        [UnitId] int NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL CONSTRAINT [DF_PhysicalVolumesBer_SortOrder] DEFAULT 0,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_PhysicalVolumesBer] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhysicalVolumesBer_IicWorkDirections_ClassifierId]
            FOREIGN KEY ([ClassifierId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PhysicalVolumesBer_PhysicalVolumesBer_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [PhysicalVolumesBer] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhysicalVolumesBer_UnitsOfMeasurement_UnitId]
            FOREIGN KEY ([UnitId]) REFERENCES [UnitsOfMeasurement] ([Id]) ON DELETE SET NULL
    );

    CREATE INDEX [IX_PhysicalVolumesBer_Code] ON [PhysicalVolumesBer]([Code]);
    CREATE INDEX [IX_PhysicalVolumesBer_ClassifierId] ON [PhysicalVolumesBer]([ClassifierId]);
    CREATE INDEX [IX_PhysicalVolumesBer_ParentId] ON [PhysicalVolumesBer]([ParentId]);
    CREATE INDEX [IX_PhysicalVolumesBer_IsArchived] ON [PhysicalVolumesBer]([IsArchived]);
    CREATE INDEX [IX_PhysicalVolumesBer_IsAssessment] ON [PhysicalVolumesBer]([IsAssessment]);
    CREATE INDEX [IX_PhysicalVolumesBer_DocumentSet] ON [PhysicalVolumesBer]([DocumentSet]);
    CREATE INDEX [IX_PhysicalVolumesBer_UnitId] ON [PhysicalVolumesBer]([UnitId]);
    CREATE INDEX [IX_PhysicalVolumesBer_IsArchived_ParentId_SortOrder]
        ON [PhysicalVolumesBer]([IsArchived], [ParentId], [SortOrder]);
    CREATE INDEX [IX_PhysicalVolumesBer_IsArchived_SortOrder_Name]
        ON [PhysicalVolumesBer]([IsArchived], [SortOrder], [Name]);
END;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumes')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo])
    BEGIN
        SET IDENTITY_INSERT [PhysicalVolumesKfo] ON;
        INSERT INTO [PhysicalVolumesKfo] (
            [Id], [Code], [Description], [ClassifierId], [IicNormId], [IsAssessment], [DocumentSet],
            [Coefficient], [UnitId], [Name], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [ModifiedAt]
        )
        SELECT
            pv.[Id], pv.[Code], pv.[Description], pv.[ClassifierId], pv.[IicNormId], pv.[IsAssessment], pv.[DocumentSet],
            pv.[Coefficient], pv.[UnitId], pv.[Name], pv.[ParentId], pv.[SortOrder], pv.[IsArchived], pv.[CreatedAt], pv.[ModifiedAt]
        FROM [PhysicalVolumes] pv
        INNER JOIN [PhysicalVolumeTypes] pvt ON pvt.[Id] = pv.[VolumeTypeId]
        WHERE pvt.[Name] LIKE N'КФО%';
        SET IDENTITY_INSERT [PhysicalVolumesKfo] OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesBer])
    BEGIN
        SET IDENTITY_INSERT [PhysicalVolumesBer] ON;
        INSERT INTO [PhysicalVolumesBer] (
            [Id], [Code], [Description], [ClassifierId], [IsAssessment], [DocumentSet],
            [Coefficient], [UnitId], [Name], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [ModifiedAt]
        )
        SELECT
            pv.[Id], pv.[Code], pv.[Description], pv.[ClassifierId], pv.[IsAssessment], pv.[DocumentSet],
            pv.[Coefficient], pv.[UnitId], pv.[Name], pv.[ParentId], pv.[SortOrder], pv.[IsArchived], pv.[CreatedAt], pv.[ModifiedAt]
        FROM [PhysicalVolumes] pv
        INNER JOIN [PhysicalVolumeTypes] pvt ON pvt.[Id] = pv.[VolumeTypeId]
        WHERE pvt.[Name] LIKE N'БЭР%';
        SET IDENTITY_INSERT [PhysicalVolumesBer] OFF;
    END;
END;
GO

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'PhysicalVolumeKfoId') IS NULL
    ALTER TABLE [ProjectPhysicalVolumes] ADD [PhysicalVolumeKfoId] int NULL;

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'PhysicalVolumeBerId') IS NULL
    ALTER TABLE [ProjectPhysicalVolumes] ADD [PhysicalVolumeBerId] int NULL;
GO

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'PhysicalVolumeId') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE ppv
    SET ppv.[PhysicalVolumeKfoId] = ppv.[PhysicalVolumeId]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumes] pv ON pv.[Id] = ppv.[PhysicalVolumeId]
    INNER JOIN [PhysicalVolumeTypes] pvt ON pvt.[Id] = pv.[VolumeTypeId]
    WHERE pvt.[Name] LIKE N''КФО%'' AND ppv.[PhysicalVolumeKfoId] IS NULL;

    UPDATE ppv
    SET ppv.[PhysicalVolumeBerId] = ppv.[PhysicalVolumeId]
    FROM [ProjectPhysicalVolumes] ppv
    INNER JOIN [PhysicalVolumes] pv ON pv.[Id] = ppv.[PhysicalVolumeId]
    INNER JOIN [PhysicalVolumeTypes] pvt ON pvt.[Id] = pv.[VolumeTypeId]
    WHERE pvt.[Name] LIKE N''БЭР%'' AND ppv.[PhysicalVolumeBerId] IS NULL;
    ');
END;
GO

IF COL_LENGTH('dbo.ProjectLaborIntensities', 'PhysicalVolumeKfoId') IS NULL
    ALTER TABLE [ProjectLaborIntensities] ADD [PhysicalVolumeKfoId] int NULL;

IF COL_LENGTH('dbo.ProjectLaborIntensities', 'PhysicalVolumeBerId') IS NULL
    ALTER TABLE [ProjectLaborIntensities] ADD [PhysicalVolumeBerId] int NULL;
GO

IF COL_LENGTH('dbo.ProjectLaborIntensities', 'PhysicalVolumeId') IS NOT NULL
BEGIN
    EXEC(N'
    UPDATE li
    SET li.[PhysicalVolumeKfoId] = li.[PhysicalVolumeId]
    FROM [ProjectLaborIntensities] li
    INNER JOIN [PhysicalVolumes] pv ON pv.[Id] = li.[PhysicalVolumeId]
    INNER JOIN [PhysicalVolumeTypes] pvt ON pvt.[Id] = pv.[VolumeTypeId]
    WHERE pvt.[Name] LIKE N''КФО%'' AND li.[PhysicalVolumeKfoId] IS NULL;

    UPDATE li
    SET li.[PhysicalVolumeBerId] = li.[PhysicalVolumeId]
    FROM [ProjectLaborIntensities] li
    INNER JOIN [PhysicalVolumes] pv ON pv.[Id] = li.[PhysicalVolumeId]
    INNER JOIN [PhysicalVolumeTypes] pvt ON pvt.[Id] = pv.[VolumeTypeId]
    WHERE pvt.[Name] LIKE N''БЭР%'' AND li.[PhysicalVolumeBerId] IS NULL;
    ');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectPhysicalVolumes_PhysicalVolumesKfo_PhysicalVolumeKfoId')
    ALTER TABLE [ProjectPhysicalVolumes] ADD CONSTRAINT [FK_ProjectPhysicalVolumes_PhysicalVolumesKfo_PhysicalVolumeKfoId]
        FOREIGN KEY ([PhysicalVolumeKfoId]) REFERENCES [PhysicalVolumesKfo] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectPhysicalVolumes_PhysicalVolumesBer_PhysicalVolumeBerId')
    ALTER TABLE [ProjectPhysicalVolumes] ADD CONSTRAINT [FK_ProjectPhysicalVolumes_PhysicalVolumesBer_PhysicalVolumeBerId]
        FOREIGN KEY ([PhysicalVolumeBerId]) REFERENCES [PhysicalVolumesBer] ([Id]) ON DELETE NO ACTION;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectLaborIntensities_PhysicalVolumesKfo_PhysicalVolumeKfoId')
    ALTER TABLE [ProjectLaborIntensities] ADD CONSTRAINT [FK_ProjectLaborIntensities_PhysicalVolumesKfo_PhysicalVolumeKfoId]
        FOREIGN KEY ([PhysicalVolumeKfoId]) REFERENCES [PhysicalVolumesKfo] ([Id]) ON DELETE SET NULL;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectLaborIntensities_PhysicalVolumesBer_PhysicalVolumeBerId')
    ALTER TABLE [ProjectLaborIntensities] ADD CONSTRAINT [FK_ProjectLaborIntensities_PhysicalVolumesBer_PhysicalVolumeBerId]
        FOREIGN KEY ([PhysicalVolumeBerId]) REFERENCES [PhysicalVolumesBer] ([Id]) ON DELETE SET NULL;

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectPhysicalVolumes_PhysicalVolumes_PhysicalVolumeId')
    ALTER TABLE [ProjectPhysicalVolumes] DROP CONSTRAINT [FK_ProjectPhysicalVolumes_PhysicalVolumes_PhysicalVolumeId];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectPhysicalVolumes_PhysicalVolumeId' AND object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes'))
    DROP INDEX [IX_ProjectPhysicalVolumes_PhysicalVolumeId] ON [ProjectPhysicalVolumes];

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId'
      AND object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes')
)
    DROP INDEX [IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId] ON [ProjectPhysicalVolumes];

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId_FacilityCode_FacilityName_WorkId'
      AND object_id = OBJECT_ID(N'dbo.ProjectPhysicalVolumes')
)
    DROP INDEX [IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeId_FacilityCode_FacilityName_WorkId]
        ON [ProjectPhysicalVolumes];

IF COL_LENGTH('dbo.ProjectPhysicalVolumes', 'PhysicalVolumeId') IS NOT NULL
    ALTER TABLE [ProjectPhysicalVolumes] DROP COLUMN [PhysicalVolumeId];
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeKfoId')
    CREATE INDEX [IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeKfoId]
        ON [ProjectPhysicalVolumes]([ProjectAnalysisVersionId], [ProjectId], [PhysicalVolumeKfoId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeBerId')
    CREATE INDEX [IX_ProjectPhysicalVolumes_ProjectAnalysisVersionId_ProjectId_PhysicalVolumeBerId]
        ON [ProjectPhysicalVolumes]([ProjectAnalysisVersionId], [ProjectId], [PhysicalVolumeBerId]);
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId')
    ALTER TABLE [ProjectLaborIntensities] DROP CONSTRAINT [FK_ProjectLaborIntensities_PhysicalVolumes_PhysicalVolumeId];

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectLaborIntensities_PhysicalVolumeId')
    DROP INDEX [IX_ProjectLaborIntensities_PhysicalVolumeId] ON [ProjectLaborIntensities];

IF COL_LENGTH('dbo.ProjectLaborIntensities', 'PhysicalVolumeId') IS NOT NULL
    ALTER TABLE [ProjectLaborIntensities] DROP COLUMN [PhysicalVolumeId];

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectLaborIntensities_PhysicalVolumeKfoId')
    CREATE INDEX [IX_ProjectLaborIntensities_PhysicalVolumeKfoId] ON [ProjectLaborIntensities]([PhysicalVolumeKfoId]);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectLaborIntensities_PhysicalVolumeBerId')
    CREATE INDEX [IX_ProjectLaborIntensities_PhysicalVolumeBerId] ON [ProjectLaborIntensities]([PhysicalVolumeBerId]);
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumes')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhysicalVolumes_IicWorkDirectionNorms_IicNormId')
        ALTER TABLE [PhysicalVolumes] DROP CONSTRAINT [FK_PhysicalVolumes_IicWorkDirectionNorms_IicNormId];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhysicalVolumes_UnitsOfMeasurement_UnitId')
        ALTER TABLE [PhysicalVolumes] DROP CONSTRAINT [FK_PhysicalVolumes_UnitsOfMeasurement_UnitId];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhysicalVolumes_IicWorkDirections_ClassifierId')
        ALTER TABLE [PhysicalVolumes] DROP CONSTRAINT [FK_PhysicalVolumes_IicWorkDirections_ClassifierId];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhysicalVolumes_PhysicalVolumeTypes_VolumeTypeId')
        ALTER TABLE [PhysicalVolumes] DROP CONSTRAINT [FK_PhysicalVolumes_PhysicalVolumeTypes_VolumeTypeId];
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhysicalVolumes_PhysicalVolumes_ParentId')
        ALTER TABLE [PhysicalVolumes] DROP CONSTRAINT [FK_PhysicalVolumes_PhysicalVolumes_ParentId];

    DROP TABLE [PhysicalVolumes];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625120000_SplitPhysicalVolumes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625120000_SplitPhysicalVolumes', N'8.0.0');
END;
GO
