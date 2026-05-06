-- ============================================================================
-- Schema 011: ProjectCharacteristics + ProjectCharacteristicOptions
--   Источник данных: .src/PEPM_metric_v.2+.xlsx, вкладка «Справочник_PD».
--   Хранит характеристики проекта (Характер проекта, Размер проекта и т.д.)
--   и их варианты со средним PD% и точностью сравнения.
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429213713_AddProjectCharacteristics'
)
BEGIN
    CREATE TABLE [ProjectCharacteristics] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ProjectCharacteristics] PRIMARY KEY ([Id])
    );

    CREATE TABLE [ProjectCharacteristicOptions] (
        [Id] int NOT NULL IDENTITY,
        [CharacteristicId] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ValuePercent] decimal(5,2) NULL,
        [ComparisonAccuracy] nvarchar(50) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ProjectCharacteristicOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectCharacteristicOptions_ProjectCharacteristics_CharacteristicId]
            FOREIGN KEY ([CharacteristicId]) REFERENCES [ProjectCharacteristics] ([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_ProjectCharacteristics_Name] ON [ProjectCharacteristics] ([Name]);
    CREATE INDEX [IX_ProjectCharacteristics_IsArchived] ON [ProjectCharacteristics] ([IsArchived]);
    CREATE INDEX [IX_ProjectCharacteristicOptions_CharacteristicId] ON [ProjectCharacteristicOptions] ([CharacteristicId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429213713_AddProjectCharacteristics', N'10.0.5');
END;
COMMIT;
GO
