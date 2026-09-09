-- 123: регистр привязки справочника трудоёмкости к группе ИИС (периоды, как нормы).
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'LaborIntensityIicBindings')
BEGIN
    CREATE TABLE [LaborIntensityIicBindings] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [LaborIntensityId] INT NOT NULL,
        [IicWorkDirectionId] INT NULL,
        [EffectiveFrom] DATETIME2 NOT NULL,
        [EffectiveTo] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_LaborIntensityIicBindings_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_LaborIntensityIicBindings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaborIntensityIicBindings_LaborIntensity]
            FOREIGN KEY ([LaborIntensityId]) REFERENCES [LaborIntensities]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_LaborIntensityIicBindings_IicWorkDirection]
            FOREIGN KEY ([IicWorkDirectionId]) REFERENCES [IicWorkDirections]([Id]),
        CONSTRAINT [CK_LaborIntensityIicBindings_EffectiveRange]
            CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom])
    );

    CREATE UNIQUE INDEX [UX_LaborIntensityIicBindings_Labor_EffectiveFrom]
        ON [LaborIntensityIicBindings] ([LaborIntensityId], [EffectiveFrom] DESC)
        INCLUDE ([EffectiveTo], [IicWorkDirectionId]);
END;

-- Исторический старт: текущая группа карточки как открытый период.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'LaborIntensityIicBindings')
BEGIN
    INSERT INTO [LaborIntensityIicBindings] (
        [LaborIntensityId], [IicWorkDirectionId], [EffectiveFrom], [EffectiveTo], [CreatedAt])
    SELECT
        li.[Id],
        li.[IicWorkDirectionId],
        CONVERT(datetime2, '2000-01-01', 126),
        NULL,
        SYSUTCDATETIME()
    FROM [LaborIntensities] li
    WHERE li.[IicWorkDirectionId] IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 FROM [LaborIntensityIicBindings] b
          WHERE b.[LaborIntensityId] = li.[Id]
      );
END;

PRINT '123_LaborIntensityIicBindings completed';
