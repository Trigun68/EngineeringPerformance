-- ============================================================================
-- Schema 010: LaborIntensities table
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260429132548_AddLaborIntensity'
)
BEGIN
    CREATE TABLE [LaborIntensities] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [NormPpr] decimal(18,4) NULL,
        [NormOpr] decimal(18,4) NULL,
        [NormPd] decimal(18,4) NULL,
        [NormRd] decimal(18,4) NULL,
        [IicWorkDirectionId] int NULL,
        [Name] nvarchar(500) NOT NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        [IsArchived] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ModifiedAt] datetime2 NULL,
        CONSTRAINT [PK_LaborIntensities] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaborIntensities_IicWorkDirections_IicWorkDirectionId]
            FOREIGN KEY ([IicWorkDirectionId]) REFERENCES [IicWorkDirections] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_LaborIntensities_LaborIntensities_ParentId]
            FOREIGN KEY ([ParentId]) REFERENCES [LaborIntensities] ([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_LaborIntensities_Code] ON [LaborIntensities] ([Code]);
    CREATE INDEX [IX_LaborIntensities_IicWorkDirectionId] ON [LaborIntensities] ([IicWorkDirectionId]);
    CREATE INDEX [IX_LaborIntensities_IsArchived] ON [LaborIntensities] ([IsArchived]);
    CREATE INDEX [IX_LaborIntensities_ParentId] ON [LaborIntensities] ([ParentId]);

    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260429132548_AddLaborIntensity', N'10.0.5');
END;
COMMIT;
GO
