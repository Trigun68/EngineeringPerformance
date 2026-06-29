-- ============================================================================
-- Восстановление таблицы PhysicalVolumes, если 017 выполнилась с пустым переносом
-- (типы БЭР24/КФО2 вместо БЭР/КФО). После этого — data/005_PhysicalVolumes.sql.
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.PhysicalVolumes', N'U') IS NULL
BEGIN
    CREATE TABLE [PhysicalVolumes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Description] nvarchar(4000) NOT NULL,
        [VolumeTypeId] int NOT NULL,
        [ClassifierId] int NULL,
        [IicNormId] int NULL,
        [IsAssessment] bit NULL,
        [DocumentSet] nvarchar(100) NULL,
        [Coefficient] decimal(18,4) NOT NULL CONSTRAINT [DF_PhysicalVolumes_Coefficient] DEFAULT 1,
        [UnitId] int NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL CONSTRAINT [DF_PhysicalVolumes_SortOrder] DEFAULT 0,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_PhysicalVolumes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PhysicalVolumes_IicWorkDirections_ClassifierId]
            FOREIGN KEY ([ClassifierId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_PhysicalVolumes_PhysicalVolumeTypes_VolumeTypeId]
            FOREIGN KEY ([VolumeTypeId]) REFERENCES [PhysicalVolumeTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PhysicalVolumes_PhysicalVolumes_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [PhysicalVolumes] ([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_PhysicalVolumes_Code] ON [PhysicalVolumes]([Code]);
    CREATE INDEX [IX_PhysicalVolumes_VolumeTypeId] ON [PhysicalVolumes]([VolumeTypeId]);
    CREATE INDEX [IX_PhysicalVolumes_ClassifierId] ON [PhysicalVolumes]([ClassifierId]);
    CREATE INDEX [IX_PhysicalVolumes_ParentId] ON [PhysicalVolumes]([ParentId]);
    CREATE INDEX [IX_PhysicalVolumes_IsArchived] ON [PhysicalVolumes]([IsArchived]);
    CREATE INDEX [IX_PhysicalVolumes_UnitId] ON [PhysicalVolumes]([UnitId]);

    IF COL_LENGTH('dbo.PhysicalVolumes', 'IicNormId') IS NOT NULL
    BEGIN
        CREATE INDEX [IX_PhysicalVolumes_IicNormId] ON [PhysicalVolumes]([IicNormId]);
    END;
END;
GO

-- Синонимы имён для idempotent seed 005 (БЭР / КФО).
IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumeTypes] WHERE [Name] = N'БЭР')
   AND EXISTS (SELECT 1 FROM [PhysicalVolumeTypes] WHERE [Name] LIKE N'БЭР%')
BEGIN
    INSERT INTO [PhysicalVolumeTypes] ([Name], [Description], [IsArchived], [CreatedAt])
    SELECT TOP 1 N'БЭР', [Description], [IsArchived], SYSUTCDATETIME()
    FROM [PhysicalVolumeTypes] WHERE [Name] LIKE N'БЭР%';
END;

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumeTypes] WHERE [Name] = N'КФО')
   AND EXISTS (SELECT 1 FROM [PhysicalVolumeTypes] WHERE [Name] LIKE N'КФО%')
BEGIN
    INSERT INTO [PhysicalVolumeTypes] ([Name], [Description], [IsArchived], [CreatedAt])
    SELECT TOP 1 N'КФО', [Description], [IsArchived], SYSUTCDATETIME()
    FROM [PhysicalVolumeTypes] WHERE [Name] LIKE N'КФО%';
END;
GO
