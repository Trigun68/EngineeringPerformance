-- ============================================================================
-- Schema 013: ProjectCharacteristicSelections
--   Привязка проекта к выбранному варианту характеристики.
--   Уникальный индекс (ProjectId, CharacteristicId) — одна характеристика
--   может быть выбрана для проекта только один раз.
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429235517_AddProjectCharacteristicSelections'
)
BEGIN
    CREATE TABLE [ProjectCharacteristicSelections] (
        [Id] int NOT NULL IDENTITY,
        [ProjectId] int NOT NULL,
        [CharacteristicId] int NOT NULL,
        [OptionId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_ProjectCharacteristicSelections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProjectCharacteristicSelections_Projects_ProjectId]
            FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ProjectCharacteristicSelections_ProjectCharacteristics_CharacteristicId]
            FOREIGN KEY ([CharacteristicId]) REFERENCES [ProjectCharacteristics] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ProjectCharacteristicSelections_ProjectCharacteristicOptions_OptionId]
            FOREIGN KEY ([OptionId]) REFERENCES [ProjectCharacteristicOptions] ([Id]) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX [IX_ProjectCharacteristicSelections_ProjectId_CharacteristicId]
        ON [ProjectCharacteristicSelections] ([ProjectId], [CharacteristicId]);
    CREATE INDEX [IX_ProjectCharacteristicSelections_CharacteristicId]
        ON [ProjectCharacteristicSelections] ([CharacteristicId]);
    CREATE INDEX [IX_ProjectCharacteristicSelections_OptionId]
        ON [ProjectCharacteristicSelections] ([OptionId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429235517_AddProjectCharacteristicSelections', N'10.0.5');
END;
COMMIT;
GO
